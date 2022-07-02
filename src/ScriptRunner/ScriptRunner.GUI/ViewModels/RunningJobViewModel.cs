using Avalonia.Threading;
using CliWrap;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

    public event EventHandler ExecutionCompleted;
    public void RaiseExecutionCompleted() => ExecutionCompleted?.Invoke(this, EventArgs.Empty);

    public void RunJob(string commandPath, string args, string? workingDirectory)
    {
        CurrentRunOutput = "";
        ExecutionPending = true;
        Task.Run(async () =>
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                await using var inputStream = new MultiplexerStream();
                inputWriter = new StreamWriter(inputStream);
                ExecutionCancellation = new CancellationTokenSource();
                ChangeStatus(RunningJobStatus.Running);
                await Cli.Wrap(commandPath)
                    .WithArguments(args)
                    //TODO: Working dir should be read from the config with the fallback set to the config file dir
                    .WithWorkingDirectory(workingDirectory ?? "Scripts/")
                    .WithStandardInputPipe(PipeSource.FromStream(inputStream,autoFlush:true))
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(AppendToOutput))
                    .WithStandardErrorPipe(PipeTarget.ToDelegate(AppendToOutput))
                    .WithValidation(CommandResultValidation.None)
                    .WithEnvironmentVariables(EnvironmentVariables)
                    .ExecuteAsync(ExecutionCancellation.Token);
                ChangeStatus(RunningJobStatus.Finished);
                Dispatcher.UIThread.Post(RaiseExecutionCompleted);
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

    private sealed class MultiplexerStream: Stream
    {
        private readonly AutoResetEvent _mre = new(false);
        private MemoryStream _temporalBuffer = new();
        private byte[]? _currentData = null;
        private int readPost;
        private bool disposed;

        public override void Flush()
        {
            _currentData = _temporalBuffer.ToArray();
            _temporalBuffer.Close();
            _temporalBuffer = new MemoryStream();
            readPost = 0;
            _mre.Set();
        }

        protected override void Dispose(bool disposing)
        {
            disposed = true;
            _mre.Set();
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_currentData == null)
            {
                if (disposed)
                {
                    return 0;
                }
                _mre.WaitOne();
                if (disposed || _currentData == null)
                {
                    return 0;
                }
            }

            var toRead = _currentData.Length - readPost > count? count: _currentData.Length - readPost;

            Array.Copy
            (
                sourceArray: _currentData,
                sourceIndex: readPost,
                destinationArray: buffer,
                destinationIndex: offset,
                length: toRead
            );

            readPost += toRead;
            if (toRead < count)
            {
                _currentData = null;
            }
            return toRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _temporalBuffer.Write(buffer, offset, count);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get; }
        public override long Position { get; set; }
    }
   

    public string InputCommand
    {
        get => _inputCommand;
        set => this.RaiseAndSetIfChanged(ref _inputCommand, value);
    }

    private void ChangeStatus(RunningJobStatus status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Status = status;
        });
    }

    private static readonly Regex ConsoleSpecialCharsPattern = new Regex(@"\u001b\[[\d;]+\w?");

    private void AppendToOutput(string? s)
    {
        if (s != null)
        {
            var newContent = ConsoleSpecialCharsPattern.Replace(s, "");
            if (string.IsNullOrEmpty(newContent))
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                
                CurrentRunOutput += newContent + Environment.NewLine;
                OutputIndex = CurrentRunOutput.Length;
            });
        }
    }

    public void AcceptCommand()
    {
        if (inputWriter != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                inputWriter.WriteLine(InputCommand);
                inputWriter.Flush();
                InputCommand = string.Empty;
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
    private StreamWriter? inputWriter;
    private string _inputCommand;

    public CancellationTokenSource ExecutionCancellation { get; set; }
    public Dictionary<string, string?> EnvironmentVariables { get; set; }
}