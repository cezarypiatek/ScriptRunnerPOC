using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Mcp;

/// <summary>
/// Result of a single MCP tool execution.
/// </summary>
public record JobResult(bool Success, int? ExitCode, TimeSpan Elapsed);

/// <summary>
/// Bridges the MCP layer to the Avalonia UI thread.
/// Serialises concurrent MCP calls so only one action runs at a time via the UI.
/// </summary>
public class McpUiBridge
{
    private readonly MainWindowViewModel _vm;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public McpUiBridge(MainWindowViewModel vm) => _vm = vm;

    public IReadOnlyList<ScriptConfig> GetActionsSnapshot() =>
        _vm.Actions.ToArray();   // snapshot; Actions is a List<ScriptConfig> on the main VM

    public async Task<JobResult> ExecuteActionAsync(
        ScriptConfig action,
        IReadOnlyDictionary<string, string> args,
        CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await ExecuteOnUiThreadAsync(action, args, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task<JobResult> ExecuteOnUiThreadAsync(
        ScriptConfig action,
        IReadOnlyDictionary<string, string> args,
        CancellationToken ct)
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
                    tcs.TrySetResult(new JobResult(false, null, TimeSpan.Zero));
                    return;
                }

                _vm.SelectedAction = match;

                // 2. Push MCP-supplied values into the freshly rendered form controls
                _vm.ApplyMcpParameterValues(args);

                // 3. Subscribe to the next job's completion event *before* invoking RunScript
                void OnCompleted(object? sender, EventArgs _)
                {
                    if (sender is RunningJobViewModel job)
                    {
                        job.ExecutionCompleted -= OnCompleted;
                        var success = job.Status == RunningJobStatus.Finished && (job.ExitCode ?? 0) == 0;
                        tcs.TrySetResult(new JobResult(success, job.ExitCode, job.Elapsed));
                    }
                }

                // Wire up before RunScript so we don't miss quick completions
                // We do this via a one-shot subscription on RunningJobs count change.
                var jobCountBefore = _vm.RunningJobs.Count;

                ct.Register(() =>
                {
                    // Try to cancel the job if it was started
                    if (_vm.RunningJobs.Count > jobCountBefore)
                    {
                        var job = _vm.RunningJobs[_vm.RunningJobs.Count - 1];
                        job.CancelExecution();
                    }
                    tcs.TrySetCanceled(ct);
                });

                // 4. Run
                _vm.RunScript();

                // 5. Grab the job that was just added and subscribe
                if (_vm.RunningJobs.Count > jobCountBefore)
                {
                    var newJob = _vm.RunningJobs[_vm.RunningJobs.Count - 1];
                    newJob.ExecutionCompleted += OnCompleted;
                }
                else
                {
                    // RunScript may have returned early (e.g., elevation required)
                    tcs.TrySetResult(new JobResult(false, null, TimeSpan.Zero));
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
