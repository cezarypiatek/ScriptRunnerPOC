using Avalonia.Threading;
using CliWrap;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using DynamicData;
using Microsoft.Extensions.ObjectPool;
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
    private const int OutputBufferSize = 5_000;
    public string Tile { get; set; }

    

    private RunningJobStatus _status;

    public RunningJobStatus Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public string ExecutedCommand { get; set; }
    public void CancelExecution()
    {
        GracefulCancellation.Cancel();
        KillAvailable = true;
    }
    
    public void Kill()
    {
        KillCancellation.Cancel();
    }

    public void DismissTroubleshootingMessage()
    {
        CurrentTroubleshootingMessage = null;
        TryPopNextAlert();
    }

    class LogForwarder:IObservable<string>, IDisposable
    {
        private IObserver<string> observer;

        public IDisposable Subscribe(IObserver<string> observer)
        {
            this.observer = observer;
            return this;
        }

        public void Write(string s) => observer.OnNext(s);
        public void Finish() => observer.OnCompleted();

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }

    public event EventHandler ExecutionCompleted;
    public void RaiseExecutionCompleted() => ExecutionCompleted?.Invoke(this, EventArgs.Empty);
    private IReadOnlyList<InteractiveInputDescription> _inputs = new List<InteractiveInputDescription>();
    public void RunJob(string commandPath, string args, string? workingDirectory,
        IReadOnlyList<InteractiveInputDescription> interactiveInputs, 
        IReadOnlyList<TroubleshootingItem> troubleshooting,
        bool useSystemShell = false)
    {

        _inputs = interactiveInputs;
        _troubleshooting = troubleshooting;
        CurrentRunOutput = "";
        ExecutionPending = true;
        
        Task.Factory.StartNew(async () =>
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var rawOutput = new StringBuilder();
            var rawErrorOutput = new StringBuilder();
            try
            {
                await using var inputStream = new MultiplexerStream();
                inputWriter = new StreamWriter(inputStream);
                GracefulCancellation = new CancellationTokenSource();
                KillCancellation = new CancellationTokenSource();
                ChangeStatus(RunningJobStatus.Running);
                var isJustLink = commandPath.StartsWith("http://") || commandPath.StartsWith("https://");
                if (useSystemShell || isJustLink)
                {
                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = commandPath,
                        Arguments = args,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    };
                    if (isJustLink)
                    {
                        processStartInfo.Verb = "open";
                    }

                    if (EnvironmentVariables != null)
                    {
                        foreach (var o in EnvironmentVariables)
                        {
                            processStartInfo.EnvironmentVariables[o.Key] = o.Value;
                        }    
                    }
                    var p = Process.Start(processStartInfo);
                    try
                    {
                        if (p != null)
                        {
                            await p.WaitForExitAsync(GracefulCancellation.Token);    
                        }
                    }
                    finally
                    {
                        if(p.HasExited == false)
                            p.Kill();
                    }
                }
                else
                {
                    await Cli.Wrap(commandPath)
                        .WithArguments(args)
                        //TODO: Working dir should be read from the config with the fallback set to the config file dir
                        .WithWorkingDirectory(workingDirectory ?? "Scripts/")
                        .WithStandardInputPipe(PipeSource.FromStream(inputStream,autoFlush:true))
                        .WithStandardOutputPipe(PipeTarget.ToDelegate(s =>
                        {
                            rawOutput.AppendLine(s);
                            AppendToOutput(s, ConsoleOutputLevel.Normal);
                        }))
                        .WithStandardErrorPipe(PipeTarget.ToDelegate(s =>
                        {
                            rawErrorOutput.Append(s);
                            AppendToOutput(s, ConsoleOutputLevel.Error);
                        }))
                        .WithValidation(CommandResultValidation.None)
                        .WithEnvironmentVariables(EnvironmentVariables ?? new())
                        .ExecuteAsync(KillCancellation.Token, GracefulCancellation.Token);
                } 
               
                ChangeStatus(RunningJobStatus.Finished);
            }
            catch (Exception e)
            {
                AppendToOutput("---------------------------------------------", ConsoleOutputLevel.Normal);
                
                if (e is not OperationCanceledException)
                {
                    AppendToOutput(e.Message, ConsoleOutputLevel.Error);
                    AppendToOutput(e.StackTrace, ConsoleOutputLevel.Error);
                    ChangeStatus(RunningJobStatus.Failed);
                }
                else
                {
                    AppendToOutput(e.Message, ConsoleOutputLevel.Warn);
                    ChangeStatus(RunningJobStatus.Cancelled);
                }
            }
            finally
            {
                stopWatch.Stop();
                AppendToOutput("---------------------------------------------", ConsoleOutputLevel.Normal);
                AppendToOutput($"Execution finished after {stopWatch.Elapsed}", ConsoleOutputLevel.Normal);
                

                Dispatcher.UIThread.Post(() =>
                {
                    ExecutionPending = false;
                    RawOutput = rawOutput.ToString();
                    RawErrorOutput = rawErrorOutput.ToString();
                    RaiseExecutionCompleted();
                    KillAvailable = false;
                });
               _logForwarder.Finish();
            }
        }, TaskCreationOptions.LongRunning);
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


    public ObservableCollection<TroubleshootingItem> Alerts { get; } = new ObservableCollection<TroubleshootingItem>();
    
    private string? _currentTroubleshootingMessage;

    public string? CurrentTroubleshootingMessage
    {
        get => _currentTroubleshootingMessage;
        set => this.RaiseAndSetIfChanged(ref _currentTroubleshootingMessage, value);
    }
    
    private TroubleShootingSeverity? _currentTroubleShootingSeverity;

    public TroubleShootingSeverity? CurrentTroubleShootingSeverity
    {
        get => _currentTroubleShootingSeverity;
        set => this.RaiseAndSetIfChanged(ref _currentTroubleShootingSeverity, value);
    }

    private void ChangeStatus(RunningJobStatus status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Status = status;
        });
    }

    private static readonly Regex ConsoleSpecialCharsPattern = new Regex(@"(\u001b\[[\d;]+\w?)", RegexOptions.Compiled);
   
    public RunningJobViewModel()
    {
        _logForwarder = new LogForwarder();

       this.outputSub =  _logForwarder
            .Buffer(TimeSpan.FromMilliseconds(200))
            .Where(x=>x.Count > 0)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(AppendToUiOutput);
       
       Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
               handler => Alerts.CollectionChanged += handler,
               handler => Alerts.CollectionChanged -= handler)
           .Where(x => x.EventArgs.Action == NotifyCollectionChangedAction.Add)
           .ObserveOn(RxApp.MainThreadScheduler)
           .Subscribe(x =>
           {
               TryPopNextAlert();
           });
    }

    private void TryPopNextAlert()
    {
        if (string.IsNullOrWhiteSpace(CurrentTroubleshootingMessage))
        {
            if (Alerts.Count > 0)
            {
                var troubleshootingItem = Alerts.First();
                CurrentTroubleshootingMessage = troubleshootingItem.AlertMessage;
                CurrentTroubleShootingSeverity = troubleshootingItem.Severity;
                Alerts.RemoveAt(0);    
            }
            
            
        }
    }



    private IBrush currentConsoleTextColor = Brushes.White;
    private IBrush currentConsoleBackgroundColor = Brushes.Transparent;

    private bool underline = false;
    private bool bold = false;
    private bool italic = false;

    public ObservableCollection<InteractiveInputItem> CurrentInteractiveInputs { get; set; } = new();

    public void ExecuteInteractiveInput(object data)
    {
        if (data is string text)
        {
          ExecuteInput(text);
          CurrentInteractiveInputs.Clear();
        }
    }

    private Dictionary<string, Regex> troubleShootingPatternCache = new();
    private Dictionary<string, Regex> inputPatternCache = new();
    
    private void AppendToOutput(string? s, ConsoleOutputLevel level)
    {
        
        if (s != null)
        {
            if (_inputs.Count > 0)
            {
                foreach (var input in _inputs)
                {
                    if (inputPatternCache.TryGetValue(input.WhenMatched, out var pattern) == false)
                    {
                        inputPatternCache[input.WhenMatched] = pattern = new Regex(input.WhenMatched, RegexOptions.Compiled);
                    }
                    var regex = pattern.Match(s);
                    if (regex.Success)
                    {
                       Dispatcher.UIThread.Post(() =>
                       {
                           CurrentInteractiveInputs.Clear();
                           CurrentInteractiveInputs.AddRange(input.Inputs);
                       });
                        break;
                    }
                }
            }
            
            if (_troubleshooting.Count > 0)
            {
                var clean = ConsoleSpecialCharsPattern.Replace(s, "");
                foreach (var input in _troubleshooting)
                {
                    if (troubleShootingPatternCache.TryGetValue(input.WhenMatched, out var pattern) == false)
                    {
                        troubleShootingPatternCache[input.WhenMatched] = pattern = new Regex(input.WhenMatched, RegexOptions.Compiled);
                    }
                    
                    
                    var regex = pattern.Match(clean);
                    if (regex.Success)
                    {
                        var res = input.AlertMessage;
                        foreach (Group group in regex.Groups)
                        {
                            res = res.Replace($"${{{group.Name}}}", group.Value);
                        }

                        Dispatcher.UIThread.Post(() =>
                        {
                            Alerts.Add(new TroubleshootingItem()
                            {
                                Severity = input.Severity,
                                AlertMessage = res
                            });
                        });
                        break;
                    }
                }
            }

            _logForwarder.Write(s);
        }
    }
    
    List<Inline> tmpInlinesForNewEntry = new List<Inline>();


    private Regex urlPattern = new Regex(@"(https?://[^\s<>\""']+)", RegexOptions.Compiled);

    private DefaultObjectPool<List<OutputElement>> _outputElementListPool = new DefaultObjectPool<List<OutputElement>>(new DefaultPooledObjectPolicy<List<OutputElement>>()
    {

    });
    private bool wasFallbackFromTransparent = false;
    private void AppendToUiOutput(IList<string> s)
    {
        List<OutputElement> _outputElements = _outputElementListPool.Get();
        foreach (var part in s.SelectMany(x=>x.Split("\r\n")).TakeLast(OutputBufferSize))
        {
            var subParts = ConsoleSpecialCharsPattern.Split(part);
            if (part.Contains("http://", StringComparison.OrdinalIgnoreCase) || part.Contains("https://", StringComparison.OrdinalIgnoreCase))
            {
                subParts = subParts.SelectMany(x => urlPattern.Split(x)).ToArray();
            }
            foreach (var chunk in subParts.Where(x=> x != string.Empty))
            {
                if (chunk.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || chunk.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    _outputElements.Add(new Link(chunk));
                    continue;
                }
                
                var subPart = chunk;
                if (subPart.EndsWith(";3m"))
                {
                    italic = true;
                    subPart = subPart.Replace(";3m", "m");
                }
                
                if (subPart.StartsWith("\u001b[", StringComparison.Ordinal))
                {
                    var foreground = subPart switch
                    {
                        "\u001b[30m" => Brushes.Black,
                        "\u001b[31m" => Brushes.DarkRed,
                        "\u001b[32m" => Brushes.Green,
                        "\u001b[33m" => Brushes.Yellow,
                        "\u001b[34m" => Brushes.Blue,
                        "\u001b[35m," => Brushes.DarkMagenta,
                        "\u001b[36m" => Brushes.DarkCyan,
                        "\u001b[37m" => Brushes.White,
                        "\u001b[90m" => ConsoleColors.BrightBlack,
                        "\u001b[91m" => ConsoleColors.BrightRed,
                        "\u001b[92m" => ConsoleColors.BrightGreen,
                        "\u001b[93m" => ConsoleColors.BrightYellow,
                        "\u001b[94m" => ConsoleColors.BrightBlue,
                        "\u001b[95m," =>ConsoleColors.BrightMagenta,
                        "\u001b[96m" => ConsoleColors.BrightCyan,
                        "\u001b[97m" => ConsoleColors.BrightWhite,
                        "\u001b[30;1m" => Brushes.Gray,
                        "\u001b[31;1m" => Brushes.Red,
                        "\u001b[32;1m" => Brushes.LightGreen,
                        "\u001b[33;1m" => Brushes.LightYellow,
                        "\u001b[34;1m" => Brushes.LightBlue,
                        "\u001b[35;1m," => Brushes.Magenta,
                        "\u001b[36;1m" => Brushes.Cyan,
                        "\u001b[37;1m" => Brushes.White,
                        "\u001b[0m" => Brushes.White,
                        _ => null
                    };

                    if (foreground != null)
                    {
                        currentConsoleTextColor = foreground;
                    }

                    var background = subPart switch
                    {
                        "\u001b[40m" => Brushes.Black,
                        "\u001b[41m" => Brushes.DarkRed,
                        "\u001b[42m" => Brushes.DarkGreen,
                        "\u001b[43m" => Brushes.Yellow,
                        "\u001b[44m" => Brushes.DarkBlue,
                        "\u001b[45m," => Brushes.DarkMagenta,
                        "\u001b[46m" => Brushes.DarkCyan,
                        "\u001b[47m" => Brushes.White,
                        "\u001b[100m" => ConsoleColors.BrightBlack,
                        "\u001b[101m" => ConsoleColors.BrightRed,
                        "\u001b[102m" => ConsoleColors.BrightGreen,
                        "\u001b[103m" => ConsoleColors.BrightYellow,
                        "\u001b[104m" => ConsoleColors.BrightBlue,
                        "\u001b[105m," =>ConsoleColors.BrightMagenta,
                        "\u001b[106m" => ConsoleColors.BrightCyan,
                        "\u001b[107m" => ConsoleColors.BrightWhite,
                        "\u001b[40;1m" => Brushes.Gray,
                        "\u001b[41;1m" => Brushes.Red,
                        "\u001b[42;1m" => Brushes.Green,
                        "\u001b[43;1m" => Brushes.LightYellow,
                        "\u001b[44;1m" => Brushes.Blue,
                        "\u001b[45;1m," => Brushes.Magenta,
                        "\u001b[46;1m" => Brushes.Cyan,
                        "\u001b[47;1m" => Brushes.White,
                        "\u001b[0m" => Brushes.Transparent,
                        _ => null
                    };

                    if (background != null)
                    {
                        currentConsoleBackgroundColor = background;
                    }

                    if (subPart == "\u001b[7m")
                    {
                        if(currentConsoleBackgroundColor == Brushes.Transparent)
                        {
                            wasFallbackFromTransparent = true;
                            (currentConsoleTextColor, currentConsoleBackgroundColor) = (Brushes.Black, currentConsoleTextColor);
                        }
                        else
                        {
                            if(wasFallbackFromTransparent)
                            {
                                wasFallbackFromTransparent = false;
                                (currentConsoleTextColor, currentConsoleBackgroundColor) = (currentConsoleBackgroundColor, Brushes.Transparent);
                            }
                            else (currentConsoleTextColor, currentConsoleBackgroundColor) = (currentConsoleBackgroundColor, currentConsoleTextColor);
                        }
                    }

                    if (subPart == "\u001b[1m")
                    {
                        bold = true;
                    }
                    else if (subPart == "\u003b[1m")
                    {
                        italic = true;
                    }
                    else if (subPart == "\u001b[4m")
                    {
                        underline = true;
                    }
                    else if (subPart == "\u001b[0m")
                    {
                        bold = false;
                        underline = false;
                        italic = false;
                    }


                    continue;
                }

                _outputElements.Add(new TextSpan(
                    Text: subPart,
                    IsBold: bold,
                    IsItalic: bold == false && italic,
                    IsUnderline: underline,
                    Foreground:currentConsoleTextColor,
                    BackGround: currentConsoleBackgroundColor
                    ));

            }

            _outputElements.Add(LineEnding.Instance);
        }

        AppendToUiOutputFinal(_outputElements);
    }
    private void AppendToUiOutputFinal(List<OutputElement> s)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _lineBreak ??= new();
            _underlineDecoration ??= new TextDecorationCollection()
            {
                new()
                {
                    Location = TextDecorationLocation.Underline,
                }
            };
            tmpInlinesForNewEntry.Clear();
            foreach (var part in s)
            {
                Inline transformed = part switch
                {
                    LineEnding => _lineBreak,
                    Link link => CreateLink(link.Text),
                    TextSpan textSpan => new Run(textSpan.Text)
                    {
                        Foreground = textSpan.Foreground,
                        Background = textSpan.BackGround,
                        FontStyle = textSpan switch
                        {
                            { IsBold: true } => FontStyle.Oblique,
                            { IsItalic: true } => FontStyle.Italic,
                            _ => FontStyle.Normal
                        },
                        TextDecorations = textSpan.IsUnderline ? _underlineDecoration : null
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(part))
                };
                tmpInlinesForNewEntry.Add(transformed);

            }

            if (RichOutput.Count + tmpInlinesForNewEntry.Count > OutputBufferSize)
            {
                var tmpNewLines = RichOutput.Concat(tmpInlinesForNewEntry).TakeLast(OutputBufferSize).ToArray();
                RichOutput.Clear();
                RichOutput.AddRange(tmpNewLines);
            }
            else RichOutput.AddRange(tmpInlinesForNewEntry);

            s.Clear();
            _outputElementListPool.Return(s);
        } );
    }

    private InlineUIContainer CreateLink(string chunk)
    {
        var link = new TextBlock
        {
            Text  = chunk,
            Cursor = _linkCursor??= new Cursor(StandardCursorType.Hand),
            Foreground = Brushes.LightBlue,
            TextDecorations = _underlineDecoration
        };
        link.PointerPressed += OnLinkOnPointerPressed;
        return new InlineUIContainer(link);
    }

    private static void OnLinkOnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if(args.GetCurrentPoint(null).Properties.IsLeftButtonPressed == false)
            return;
        
        if (sender is TextBlock { Text: { } url })
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return;
                Process.Start("open", url);
            }
        }
    }

    public void AcceptCommand()
    {
        if (inputWriter != null)
        {
            var inputCommand = InputCommand;
            ExecuteInput(inputCommand);
            Dispatcher.UIThread.Post(() => {
              
                InputCommand = string.Empty;
            });
        }
    }

    private void ExecuteInput(string inputCommand)
    {
        if (inputWriter != null)
        {
            inputWriter.WriteLine(inputCommand);
            inputWriter.Flush();
        }
    }


    private string _currentRunOutput;


    public enum ConsoleOutputLevel
    {
        Normal,
        Warn,
        Error
    }
    
    public string CurrentRunOutput
    {
        get => _currentRunOutput;
        set => this.RaiseAndSetIfChanged(ref _currentRunOutput, value);
    }
    
    private string _currentRunOutputBuffered;


    public int NumberOfLines
    {
        get => _numberOfLines;
        set => this.RaiseAndSetIfChanged(ref _numberOfLines, value);
    }

    public string CurrentRunOutputBuffered
    {
        get => _currentRunOutputBuffered;
        set => this.RaiseAndSetIfChanged(ref _currentRunOutputBuffered, value);
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

    private bool _killAvailable;

    public bool KillAvailable
    {
        get => _killAvailable;
        set => this.RaiseAndSetIfChanged(ref _killAvailable, value);
    }
    

    public string RawOutput { get; set; }
    public string RawErrorOutput { get; set; }

    private int _outputIndex;
    private bool _executionPending;
    private StreamWriter? inputWriter;
    private string _inputCommand;
    private int _numberOfLines;
    private readonly IDisposable outputSub;
    private IReadOnlyList<TroubleshootingItem> _troubleshooting = Array.Empty<TroubleshootingItem>();
    private LineBreak? _lineBreak;
    private readonly LogForwarder _logForwarder;
    private TextDecorationCollection? _underlineDecoration;
    private Cursor? _linkCursor;

    public CancellationTokenSource GracefulCancellation { get; set; }
    public CancellationTokenSource KillCancellation { get; set; }
    public Dictionary<string, string?> EnvironmentVariables { get; set; }

    public InlineCollection RichOutput { get; set; } = new();
   
}

public enum TroubleShootingSeverity
{
    Error,
    Warning,
    Info,
    Success
}

class TroubleShootingElement
{
    public TroubleShootingSeverity Severity { get; set; }
    public string Message { get; set; }
}