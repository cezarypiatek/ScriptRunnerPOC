using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Mcp;

/// <summary>
/// Result of a single MCP tool execution.
/// </summary>
/// <param name="Rejected">True when the user rejected the action in safe mode (no execution took place).</param>
/// <param name="StillRunning">True when fire-and-forget mode timed out before the job finished; the job continues in the background.</param>
public record JobResult(bool Success, int? ExitCode, TimeSpan Elapsed, string Output, bool Rejected = false, bool StillRunning = false);

/// <summary>
/// Bridges the MCP layer to the Avalonia UI thread.
/// Serialises concurrent MCP calls so only one action runs at a time via the UI.
/// </summary>
public class McpUiBridge
{
    private readonly MainWindowViewModel _vm;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// After launch, if the job has not completed within this window the MCP call returns
    /// a "running in background" response and the lock is released immediately.
    /// </summary>
    private static readonly TimeSpan FireAndForgetDelay = TimeSpan.FromSeconds(3);

    public McpUiBridge(MainWindowViewModel vm) => _vm = vm;

    public IReadOnlyList<ScriptConfig> GetActionsSnapshot() =>
        _vm.Actions.ToArray();   // snapshot; Actions is a List<ScriptConfig> on the main VM

    public async Task<JobResult> ExecuteActionAsync(
        ScriptConfig action,
        IReadOnlyDictionary<string, string> args,
        CancellationToken ct,
        bool safeMode = false,
        IReadOnlySet<string>? explicitKeys = null,
        bool fireAndForget = false)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!fireAndForget)
            {
                return await ExecuteOnUiThreadAsync(action, args, ct, safeMode, explicitKeys,
                    startedTcs: null, detach: null);
            }

            // Fire-and-forget path ------------------------------------------------
            // startedTcs is signalled the moment RunScript() is actually called so the
            // 3-second window begins only when execution truly starts (after any safe-mode
            // approval).  detach prevents the ct.Register callback from cancelling a job
            // that we intentionally released.
            var startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var detach = new StrongBox<bool>(false);

            var completionTask = ExecuteOnUiThreadAsync(action, args, ct, safeMode, explicitKeys,
                startedTcs, detach);

            // Phase 1: wait until execution starts (handles safe-mode approval delay)
            // or until the job completes/is rejected before even starting.
            var firstDone = await Task.WhenAny(completionTask, startedTcs.Task);
            if (firstDone == completionTask)
            {
                // Completed (or rejected) before RunScript was even called — return real result.
                return await completionTask;
            }

            // Phase 2: execution has started — race against the 3-second timer.
            using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var winner = await Task.WhenAny(completionTask, Task.Delay(FireAndForgetDelay, delayCts.Token));

            if (winner == completionTask)
            {
                // Job finished within the window — cancel the delay and return real result.
                delayCts.Cancel();
                return await completionTask;
            }

            // Timeout: mark as detached so the ct.Register callback won't kill the running job,
            // then release the lock immediately.
            detach.Value = true;

            // Observe any future exception to prevent UnobservedTaskException noise.
            _ = completionTask.ContinueWith(
                t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            return new JobResult(true, null, TimeSpan.Zero, string.Empty, StillRunning: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task<JobResult> ExecuteOnUiThreadAsync(
        ScriptConfig action,
        IReadOnlyDictionary<string, string> args,
        CancellationToken ct,
        bool safeMode,
        IReadOnlySet<string>? explicitKeys,
        TaskCompletionSource<bool>? startedTcs,
        StrongBox<bool>? detach)
    {
        var tcs = new TaskCompletionSource<JobResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // 1. Select the action — this triggers RenderParameterForm synchronously
                var match = _vm.Actions.Find(a => a.Name == action.Name && a.SourceName == action.SourceName);
                if (match is null)
                {
                    tcs.TrySetResult(new JobResult(false, null, TimeSpan.Zero, string.Empty));
                    return;
                }

                _vm.SelectedAction = match;

                // 2. Push MCP-supplied values into the freshly rendered form controls
                _vm.ApplyMcpParameterValues(args, safeMode ? explicitKeys : null);

                // 3. Subscribe to the next job's completion event *before* invoking RunScript
                void OnCompleted(object? sender, EventArgs _)
                {
                    if (sender is RunningJobViewModel job)
                    {
                        job.ExecutionCompleted -= OnCompleted;
                        var success = job.Status == RunningJobStatus.Finished && (job.ExitCode ?? 0) == 0;
                        var exitCode = job.ExitCode;
                        var elapsed = job.Elapsed;
                        // Defer reading RichOutput by one background-priority tick so the 200 ms
                        // Rx buffer in RunningJobViewModel has flushed its final batch.
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var output = job.RichOutput.Text ?? string.Empty;
                            tcs.TrySetResult(new JobResult(success, exitCode, elapsed, output));
                        }, Avalonia.Threading.DispatcherPriority.Background);
                    }
                }

                var jobCountBefore = _vm.RunningJobs.Count;

                ct.Register(() =>
                {
                    // If we've been detached (fire-and-forget timeout), leave the running job alone.
                    if (detach?.Value == true) return;

                    // Try to cancel the job if it was started, or cancel a pending safe-mode approval
                    Dispatcher.UIThread.Post(() => _vm.CancelMcpApproval());

                    if (_vm.RunningJobs.Count > jobCountBefore)
                    {
                        var job = _vm.RunningJobs[_vm.RunningJobs.Count - 1];
                        job.CancelExecution();
                    }
                    tcs.TrySetCanceled(ct);
                });

                if (!safeMode)
                {
                    // Normal (non-safe) path: execute immediately

                    // 4. Run
                    _vm.RunScript();

                    // Signal that execution has started (fire-and-forget timer begins here).
                    startedTcs?.TrySetResult(true);

                    // 5. Grab the job that was just added and subscribe
                    if (_vm.RunningJobs.Count > jobCountBefore)
                    {
                        var newJob = _vm.RunningJobs[_vm.RunningJobs.Count - 1];
                        newJob.ExecutionCompleted += OnCompleted;
                    }
                    else
                    {
                        // RunScript may have returned early (e.g., elevation required)
                        tcs.TrySetResult(new JobResult(false, null, TimeSpan.Zero, string.Empty));
                    }
                }
                else
                {
                    // Safe-mode path: defer execution to manual Accept/Reject by the user.
                    _vm.BeginMcpApproval(
                        onAccept: () =>
                        {
                            // User clicked Accept — run the script and wire completion
                            _vm.RunScript();

                            // Signal that execution has started (fire-and-forget timer begins here,
                            // after the user has approved — not at the original call time).
                            startedTcs?.TrySetResult(true);

                            if (_vm.RunningJobs.Count > jobCountBefore)
                            {
                                var newJob = _vm.RunningJobs[_vm.RunningJobs.Count - 1];
                                newJob.ExecutionCompleted += OnCompleted;
                            }
                            else
                            {
                                tcs.TrySetResult(new JobResult(false, null, TimeSpan.Zero, string.Empty));
                            }
                        },
                        onReject: () =>
                        {
                            // User clicked Reject — return a rejected result immediately.
                            // startedTcs is NOT signalled: fire-and-forget timer never started,
                            // so Phase 1 in ExecuteActionAsync will see completionTask finish first.
                            tcs.TrySetResult(new JobResult(false, null, TimeSpan.Zero, string.Empty, Rejected: true));
                        });
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }
}
