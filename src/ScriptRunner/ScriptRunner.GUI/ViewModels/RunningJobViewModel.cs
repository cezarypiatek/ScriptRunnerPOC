using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CliWrap;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ViewModels;

public enum RunningJobStatus
{
    NotStarted,
    Running,
    Cancelled,
    Failed,
    Finished
}


public class RunningJobViewModel : ViewModelBase
{
    public string Tile { get; set; }


    private RunningJobStatus _status;

    public RunningJobStatus Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public string CommandName { get; set; }
    public string ExecutedCommand { get; set; }
    public void CancelExecution() => ExecutionCancellation.Cancel();

    public void RunJob(string commandPath, string args, ScriptConfig selectedAction)
    {
        CurrentRunOutput = "";
        ExecutionPending = true;
        Task.Run(async () =>
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                ExecutionCancellation = new CancellationTokenSource();
                ChangeStatus(RunningJobStatus.Running);
                await Cli.Wrap(commandPath)
                    .WithArguments(args)
                    //TODO: Working dir should be read from the config with the fallback set to the config file dir
                    .WithWorkingDirectory(selectedAction.WorkingDirectory ?? "Scripts/")
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(AppendToOutput))
                    .WithStandardErrorPipe(PipeTarget.ToDelegate(AppendToOutput))
                    .WithValidation(CommandResultValidation.None)
                   
                    .ExecuteAsync(ExecutionCancellation.Token);
                ChangeStatus(RunningJobStatus.Finished);
            }
            catch (Exception e)
            {
                AppendToOutput("---------------------------------------------");
                AppendToOutput(e.Message);
                if (e is not OperationCanceledException)
                {
                    AppendToOutput(e.StackTrace);
                    ChangeStatus(RunningJobStatus.Failed);
                }
                else
                {
                    ChangeStatus(RunningJobStatus.Cancelled);
                }
            }
            finally
            {
                stopWatch.Stop();
                AppendToOutput("---------------------------------------------");
                AppendToOutput($"Execution finished after {stopWatch.Elapsed}");
                Dispatcher.UIThread.Post(() => { ExecutionPending = false; });
            }
        });
    }

    private void ChangeStatus(RunningJobStatus status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Status = status;
        });
    }

    private void AppendToOutput(string? s)
    {
        if (s != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                CurrentRunOutput += s + Environment.NewLine;
                OutputIndex = CurrentRunOutput.Length;
            });
        }
    }

    public string CurrentRunOutput
    {
        get => _currentRunOutput;
        set => this.RaiseAndSetIfChanged(ref _currentRunOutput, value);
    }

    public int OutputIndex
    {
        get => _outputIndex;
        set => this.RaiseAndSetIfChanged(ref _outputIndex, value);
    }

    public bool ExecutionPending
    {
        get => _executionPending;
        set => this.RaiseAndSetIfChanged(ref _executionPending, value);
    }
    private string _currentRunOutput;
    private int _outputIndex;
    private bool _executionPending;

    public CancellationTokenSource ExecutionCancellation { get; set; }
}