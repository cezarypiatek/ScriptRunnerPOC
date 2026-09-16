using Avalonia.Threading;
using CliWrap;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.ScriptConfigs;
using AvaloniaEdit.Document;

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
    public InlineCollection ExecutedCommandFormatted { get; set; } = new();
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

    /// <summary>Exit code from the last execution. 0 = success, non-zero = failure, null = not finished or shell-launched.</summary>
    public int? ExitCode { get; private set; }

    /// <summary>Total wall-clock time of the last execution.</summary>
    public TimeSpan Elapsed { get; private set; }

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
            var startedOn = DateTime.Now;
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
                            ExitCode = p.ExitCode;
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
                    var result = await Cli.Wrap(commandPath)
                        .WithArguments(args)
                        //TODO: Working dir should be read from the config with the fallback set to the config file dir
                        .WithWorkingDirectory(workingDirectory ?? "Scripts/")
                        .WithStandardInputPipe(PipeSource.FromStream(inputStream,autoFlush:true))
                        .WithStandardOutputPipe(PipeTarget.ToDelegate(s =>
                        {
                            rawOutput.AppendLine(s);
                            AppendToOutput(s, ConsoleOutputLevel.Normal);
                        }, Encoding.UTF8))
                        .WithStandardErrorPipe(PipeTarget.ToDelegate(s =>
                        {
                            rawErrorOutput.Append(s);
                            AppendToOutput(s, ConsoleOutputLevel.Error);
                        }, Encoding.UTF8))
                        .WithValidation(CommandResultValidation.None)
                        .WithEnvironmentVariables(EnvironmentVariables ?? new())
                        .ExecuteAsync(KillCancellation.Token, GracefulCancellation.Token);
                    ExitCode = result.ExitCode;
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
                Elapsed = stopWatch.Elapsed;
                AppendToOutput("---------------------------------------------", ConsoleOutputLevel.Normal);
                AppendToOutput($"Started at {startedOn} · Duration: {stopWatch.Elapsed:hh\\:mm\\:ss\\.fff}", ConsoleOutputLevel.Normal);
                

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
    private IBrush? currentUnderlineColor;

    private bool underline = false;
    private bool doubleUnderline = false;
    private bool bold = false;
    private bool faint = false;
    private bool italic = false;
    private bool strikethrough = false;
    private bool overline = false;
    private bool inverse = false;
    private bool concealed = false;

    public ObservableCollection<InteractiveInputItem> CurrentInteractiveInputs { get; set; } = new();

    public void ExecuteInteractiveInput(object data)
    {
        if (data is string text)
        {
          ExecuteInput(text);
          CurrentInteractiveInputs.Clear();
        }
    }

    private ConcurrentDictionary<string, Regex> troubleShootingPatternCache = new();
    private ConcurrentDictionary<string, Regex> inputPatternCache = new();
    
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
                var clean = VtOutputTokenizer.StripControlSequences(s);
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

    private DefaultObjectPool<List<OutputElement>> _outputElementListPool = new DefaultObjectPool<List<OutputElement>>(new DefaultPooledObjectPolicy<List<OutputElement>>()
    {

    });
    private string? currentHyperlinkTarget;
    private readonly VtOutputStreamTokenizer vtStreamTokenizer = new();

    internal void ApplyCsiSequence(string sequence)
    {
        if (!TryGetCsiCommand(sequence, out var parameters, out var command) || command != 'm')
        {
            return;
        }

        if (parameters.Length == 0)
        {
            ResetSgr();
            return;
        }

        var groups = parameters.Split(';');
        for (var index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            if (group.Contains(':'))
            {
                ApplyColonSgr(group);
                continue;
            }

            var code = ParseSgrValue(group, 0);
            if (code is 38 or 48 or 58)
            {
                index += ApplyExtendedColor(groups, index, code);
                continue;
            }

            ApplySgrCode(code);
        }
    }

    private static bool TryGetCsiCommand(string sequence, out string parameters, out char command)
    {
        parameters = string.Empty;
        command = '\0';
        if (sequence.Length < 2)
        {
            return false;
        }

        var parameterStart = sequence.StartsWith("\u001b[", StringComparison.Ordinal) ? 2 :
            sequence[0] == '\u009b' ? 1 : -1;
        if (parameterStart < 0)
        {
            return false;
        }

        command = sequence[^1];
        parameters = sequence[parameterStart..^1];
        return true;
    }

    private void ApplyColonSgr(string group)
    {
        var values = group.Split(':');
        var code = ParseSgrValue(values[0], 0);
        if (code is not (38 or 48 or 58) || values.Length < 3)
        {
            ApplySgrCode(code);
            return;
        }

        var mode = ParseSgrValue(values[1], -1);
        if (mode == 5 && TryParseByte(values[^1], out var paletteIndex))
        {
            SetExtendedColor(code, GetIndexedColor(paletteIndex));
        }
        else if (mode == 2 && values.Length >= 5 &&
                 TryParseByte(values[^3], out var red) &&
                 TryParseByte(values[^2], out var green) &&
                 TryParseByte(values[^1], out var blue))
        {
            SetExtendedColor(code, new ImmutableSolidColorBrush(Color.FromRgb(red, green, blue)));
        }
    }

    private int ApplyExtendedColor(string[] groups, int index, int code)
    {
        if (index + 2 >= groups.Length)
        {
            return 0;
        }

        var mode = ParseSgrValue(groups[index + 1], -1);
        if (mode == 5 && TryParseByte(groups[index + 2], out var paletteIndex))
        {
            SetExtendedColor(code, GetIndexedColor(paletteIndex));
            return 2;
        }

        if (mode == 2 && index + 4 < groups.Length &&
            TryParseByte(groups[index + 2], out var red) &&
            TryParseByte(groups[index + 3], out var green) &&
            TryParseByte(groups[index + 4], out var blue))
        {
            SetExtendedColor(code, new ImmutableSolidColorBrush(Color.FromRgb(red, green, blue)));
            return 4;
        }

        return 0;
    }

    private void SetExtendedColor(int code, IBrush brush)
    {
        switch (code)
        {
            case 38:
                currentConsoleTextColor = brush;
                break;
            case 48:
                currentConsoleBackgroundColor = brush;
                break;
            case 58:
                currentUnderlineColor = brush;
                break;
        }
    }

    private void ApplySgrCode(int code)
    {
        switch (code)
        {
            case 0:
                ResetSgr();
                break;
            case 1:
                bold = true;
                break;
            case 2:
                faint = true;
                break;
            case 3:
                italic = true;
                break;
            case 4:
                underline = true;
                doubleUnderline = false;
                break;
            case 5:
            case 6:
                // Blinking is intentionally rendered as steady text.
                break;
            case 7:
                inverse = true;
                break;
            case 8:
                concealed = true;
                break;
            case 9:
                strikethrough = true;
                break;
            case 21:
                underline = true;
                doubleUnderline = true;
                break;
            case 22:
                bold = false;
                faint = false;
                break;
            case 23:
                italic = false;
                break;
            case 24:
                underline = false;
                doubleUnderline = false;
                break;
            case 25:
                break;
            case 27:
                inverse = false;
                break;
            case 28:
                concealed = false;
                break;
            case 29:
                strikethrough = false;
                break;
            case >= 30 and <= 37:
                currentConsoleTextColor = GetBasicColor(code - 30, bright: false);
                break;
            case 39:
                currentConsoleTextColor = Brushes.White;
                break;
            case >= 40 and <= 47:
                currentConsoleBackgroundColor = GetBasicColor(code - 40, bright: false);
                break;
            case 49:
                currentConsoleBackgroundColor = Brushes.Transparent;
                break;
            case 53:
                overline = true;
                break;
            case 55:
                overline = false;
                break;
            case 59:
                currentUnderlineColor = null;
                break;
            case >= 90 and <= 97:
                currentConsoleTextColor = GetBasicColor(code - 90, bright: true);
                break;
            case >= 100 and <= 107:
                currentConsoleBackgroundColor = GetBasicColor(code - 100, bright: true);
                break;
        }
    }

    private void ResetSgr()
    {
        currentConsoleTextColor = Brushes.White;
        currentConsoleBackgroundColor = Brushes.Transparent;
        currentUnderlineColor = null;
        bold = false;
        faint = false;
        italic = false;
        underline = false;
        doubleUnderline = false;
        strikethrough = false;
        overline = false;
        inverse = false;
        concealed = false;
    }

    internal OutputTextStyle GetCurrentTextStyle()
    {
        var foreground = currentConsoleTextColor;
        var background = currentConsoleBackgroundColor;

        if (inverse)
        {
            (foreground, background) = background == Brushes.Transparent
                ? (Brushes.Black, foreground)
                : (background, foreground);
        }

        if (faint && foreground is ISolidColorBrush solidForeground)
        {
            foreground = new ImmutableSolidColorBrush(solidForeground.Color, 0.55);
        }

        if (concealed)
        {
            foreground = background == Brushes.Transparent ? Brushes.Transparent : background;
        }

        return new OutputTextStyle(
            foreground,
            background,
            bold,
            italic,
            underline,
            doubleUnderline,
            strikethrough,
            overline,
            currentUnderlineColor);
    }

    private static int ParseSgrValue(string value, int defaultValue) =>
        int.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static bool TryParseByte(string value, out byte result) =>
        byte.TryParse(value, out result);

    private static IBrush GetBasicColor(int index, bool bright) =>
        ConsoleColors.Get(index, bright);

    private static IBrush GetIndexedColor(byte index)
    {
        if (index < 16)
        {
            return GetBasicColor(index % 8, index >= 8);
        }

        if (index < 232)
        {
            var value = index - 16;
            var red = GetColorCubeComponent(value / 36);
            var green = GetColorCubeComponent(value / 6 % 6);
            var blue = GetColorCubeComponent(value % 6);
            return new ImmutableSolidColorBrush(Color.FromRgb(red, green, blue));
        }

        var gray = (byte)(8 + (index - 232) * 10);
        return new ImmutableSolidColorBrush(Color.FromRgb(gray, gray, gray));
    }

    private static byte GetColorCubeComponent(int component) =>
        component == 0 ? (byte)0 : (byte)(55 + component * 40);

    private void AppendToUiOutput(IList<string> s)
    {
        List<OutputElement> _outputElements = _outputElementListPool.Get();
        foreach (var part in s.SelectMany(x=>x.Split("\r\n")).TakeLast(OutputBufferSize))
        {
            var lineCells = new List<TerminalCell>();
            var cursor = 0;
            foreach (var token in vtStreamTokenizer.TokenizeChunk(part))
            {
                if (token.Kind == VtOutputTokenKind.HyperlinkStart)
                {
                    currentHyperlinkTarget = token.Target;
                    continue;
                }

                if (token.Kind == VtOutputTokenKind.HyperlinkEnd)
                {
                    currentHyperlinkTarget = null;
                    continue;
                }

                if (token.Kind == VtOutputTokenKind.Control)
                {
                    continue;
                }

                var subPart = token.Text;
                if (token.Kind == VtOutputTokenKind.Csi)
                {
                    if (!ApplyLineControl(subPart, lineCells, ref cursor))
                    {
                        ApplyCsiSequence(subPart);
                    }
                    continue;
                }

                WriteTerminalText(
                    lineCells,
                    ref cursor,
                    subPart,
                    GetCurrentTextStyle(),
                    currentHyperlinkTarget);

            }

            FlushTerminalLine(_outputElements, lineCells);
            if (!vtStreamTokenizer.HasPendingSequence)
            {
                _outputElements.Add(LineEnding.Instance);
            }
        }

        AppendToUiOutputFinal(_outputElements);
    }

    internal static void WriteTerminalText(
        List<TerminalCell> cells,
        ref int cursor,
        string text,
        OutputTextStyle style,
        string? linkTarget)
    {
        foreach (var character in text)
        {
            switch (character)
            {
                case '\r':
                    cursor = 0;
                    continue;
                case '\b':
                    cursor = Math.Max(0, cursor - 1);
                    continue;
                case '\t':
                    var spaces = 8 - cursor % 8;
                    for (var index = 0; index < spaces; index++)
                    {
                        WriteTerminalCharacter(cells, ref cursor, ' ', style, linkTarget);
                    }
                    continue;
                case '\0':
                case '\u0007':
                    continue;
                default:
                    WriteTerminalCharacter(cells, ref cursor, character, style, linkTarget);
                    break;
            }
        }
    }

    private static void WriteTerminalCharacter(
        List<TerminalCell> cells,
        ref int cursor,
        char character,
        OutputTextStyle style,
        string? linkTarget)
    {
        while (cells.Count < cursor)
        {
            cells.Add(new TerminalCell(' ', style, null));
        }

        var cell = new TerminalCell(character, style, linkTarget);
        if (cursor < cells.Count)
        {
            cells[cursor] = cell;
        }
        else
        {
            cells.Add(cell);
        }

        cursor++;
    }

    internal bool ApplyLineControl(string sequence, List<TerminalCell> cells, ref int cursor)
    {
        if (!TryGetCsiCommand(sequence, out var parameters, out var command) || command == 'm')
        {
            return false;
        }

        var value = ParseSgrValue(parameters.Split(';')[0], command is 'G' or 'C' or 'D' or 'X' ? 1 : 0);
        switch (command)
        {
            case 'G': // Cursor Horizontal Absolute
                cursor = Math.Max(0, value - 1);
                return true;
            case 'C': // Cursor Forward
                cursor += Math.Max(1, value);
                return true;
            case 'D': // Cursor Backward
                cursor = Math.Max(0, cursor - Math.Max(1, value));
                return true;
            case 'K': // Erase in Line
                EraseInLine(cells, cursor, value);
                return true;
            case 'X': // Erase Character
                var eraseCount = Math.Max(1, value);
                var eraseStyle = GetCurrentTextStyle();
                for (var index = cursor; index < Math.Min(cells.Count, cursor + eraseCount); index++)
                {
                    cells[index] = new TerminalCell(' ', eraseStyle, null);
                }
                return true;
            default:
                // Other cursor/screen controls are intentionally consumed.
                return true;
        }
    }

    private void EraseInLine(List<TerminalCell> cells, int cursor, int mode)
    {
        var eraseStyle = GetCurrentTextStyle();
        switch (mode)
        {
            case 0:
                if (cursor < cells.Count)
                {
                    cells.RemoveRange(cursor, cells.Count - cursor);
                }
                break;
            case 1:
                for (var index = 0; index < Math.Min(cells.Count, cursor + 1); index++)
                {
                    cells[index] = new TerminalCell(' ', eraseStyle, null);
                }
                break;
            case 2:
                cells.Clear();
                break;
        }
    }

    private void FlushTerminalLine(List<OutputElement> outputElements, List<TerminalCell> cells)
    {
        var start = 0;
        while (start < cells.Count)
        {
            var first = cells[start];
            var end = start + 1;
            while (end < cells.Count &&
                   cells[end].Style == first.Style &&
                   cells[end].LinkTarget == first.LinkTarget)
            {
                end++;
            }

            var text = new string(cells.Skip(start).Take(end - start).Select(cell => cell.Character).ToArray());
            if (first.LinkTarget is not null)
            {
                outputElements.Add(new Link(
                    text,
                    first.LinkTarget,
                    first.Style,
                    UseLinkAppearance: false));
            }
            else
            {
                AddTextWithLinks(outputElements, text, first.Style);
            }

            start = end;
        }
    }

    private void AddTextWithLinks(List<OutputElement> outputElements, string text, OutputTextStyle style)
    {
        var currentOffset = 0;
        foreach (var match in OutputLinkDetector.Detect(text))
        {
            if (match.Start > currentOffset)
            {
                AddTextSpan(outputElements, text[currentOffset..match.Start], style);
            }

            outputElements.Add(new Link(
                text.Substring(match.Start, match.Length),
                match.Target,
                style));
            currentOffset = match.Start + match.Length;
        }

        if (currentOffset < text.Length)
        {
            AddTextSpan(outputElements, text[currentOffset..], style);
        }
    }

    private static void AddTextSpan(List<OutputElement> outputElements, string text, OutputTextStyle style)
    {
        if (text.Length == 0)
        {
            return;
        }

        outputElements.Add(new TextSpan(text, style));
    }

    internal readonly record struct TerminalCell(
        char Character,
        OutputTextStyle Style,
        string? LinkTarget);
    private void AppendToUiOutputFinal(List<OutputElement> s)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var sb = new StringBuilder();
            var newSegments = new List<FormattedSegment>();
            
            foreach (var part in s)
            {
                int startOffset = sb.Length;
                
                switch (part)
                {
                    case LineEnding:
                        sb.AppendLine();
                        break;
                    
                    case Link link:
                        sb.Append(link.Text);
                        newSegments.Add(new FormattedSegment
                        {
                            StartOffset = startOffset,
                            Length = link.Text.Length,
                            Foreground = link.UseLinkAppearance
                                ? Brushes.LightBlue
                                : link.Style.Foreground,
                            Background = link.Style.Background,
                            IsBold = link.Style.IsBold,
                            IsItalic = link.Style.IsItalic,
                            IsUnderline = link.UseLinkAppearance || link.Style.IsUnderline,
                            IsDoubleUnderline = link.Style.IsDoubleUnderline,
                            IsStrikethrough = link.Style.IsStrikethrough,
                            IsOverline = link.Style.IsOverline,
                            UnderlineColor = link.Style.UnderlineColor,
                            IsLink = true,
                            LinkUrl = link.Url ?? link.Text
                        });
                        break;
                    
                    case TextSpan textSpan:
                        sb.Append(textSpan.Text);
                        
                        // Try to merge with the last segment if formatting is identical
                        var lastSegment = newSegments.Count > 0 ? newSegments[^1] : null;
                        if (lastSegment != null && 
                            !lastSegment.IsLink &&
                            lastSegment.StartOffset + lastSegment.Length == startOffset &&
                            ReferenceEquals(lastSegment.Foreground, textSpan.Style.Foreground) &&
                            ReferenceEquals(lastSegment.Background, textSpan.Style.Background) &&
                            lastSegment.IsBold == textSpan.Style.IsBold &&
                            lastSegment.IsItalic == textSpan.Style.IsItalic &&
                            lastSegment.IsUnderline == textSpan.Style.IsUnderline &&
                            lastSegment.IsDoubleUnderline == textSpan.Style.IsDoubleUnderline &&
                            lastSegment.IsStrikethrough == textSpan.Style.IsStrikethrough &&
                            lastSegment.IsOverline == textSpan.Style.IsOverline &&
                            ReferenceEquals(lastSegment.UnderlineColor, textSpan.Style.UnderlineColor))
                        {
                            // Merge with previous segment by extending its length
                            lastSegment.Length += textSpan.Text.Length;
                        }
                        else
                        {
                            // Create new segment
                            newSegments.Add(new FormattedSegment
                            {
                                StartOffset = startOffset,
                                Length = textSpan.Text.Length,
                                Foreground = textSpan.Style.Foreground,
                                Background = textSpan.Style.Background,
                                IsBold = textSpan.Style.IsBold,
                                IsItalic = textSpan.Style.IsItalic,
                                IsUnderline = textSpan.Style.IsUnderline,
                                IsDoubleUnderline = textSpan.Style.IsDoubleUnderline,
                                IsStrikethrough = textSpan.Style.IsStrikethrough,
                                IsOverline = textSpan.Style.IsOverline,
                                UnderlineColor = textSpan.Style.UnderlineColor,
                                IsLink = false
                            });
                        }
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(part));
                }
            }

            var newText = sb.ToString();
            
            // Handle buffer size limit
            if (RichOutput.TextLength + newText.Length > OutputBufferSize * 100)
            {
                var excessLength = (RichOutput.TextLength + newText.Length) - (OutputBufferSize * 100);
                if (excessLength < RichOutput.TextLength)
                {
                    RichOutput.Remove(0, excessLength);
                    // Adjust existing segments
                    for (int i = _formattingSegments.Count - 1; i >= 0; i--)
                    {
                        var seg = _formattingSegments[i];
                        if (seg.StartOffset + seg.Length <= excessLength)
                        {
                            _formattingSegments.RemoveAt(i);
                        }
                        else if (seg.StartOffset < excessLength)
                        {
                            seg.Length -= (excessLength - seg.StartOffset);
                            seg.StartOffset = 0;
                        }
                        else
                        {
                            seg.StartOffset -= excessLength;
                        }
                    }
                }
                else
                {
                    RichOutput.Text = string.Empty;
                    _formattingSegments.Clear();
                }
            }
            
            int baseOffset = RichOutput.TextLength;
            RichOutput.Insert(RichOutput.TextLength, newText);
            
            // Add new segments with adjusted offsets and merge with last existing segment if possible
            foreach (var segment in newSegments)
            {
                segment.StartOffset += baseOffset;
                
                // Try to merge with the very last segment in the existing list
                var lastExistingSegment = _formattingSegments.Count > 0 ? _formattingSegments[^1] : null;
                if (lastExistingSegment != null &&
                    !lastExistingSegment.IsLink &&
                    !segment.IsLink &&
                    lastExistingSegment.StartOffset + lastExistingSegment.Length == segment.StartOffset &&
                    ReferenceEquals(lastExistingSegment.Foreground, segment.Foreground) &&
                    ReferenceEquals(lastExistingSegment.Background, segment.Background) &&
                    lastExistingSegment.IsBold == segment.IsBold &&
                    lastExistingSegment.IsItalic == segment.IsItalic &&
                    lastExistingSegment.IsUnderline == segment.IsUnderline &&
                    lastExistingSegment.IsDoubleUnderline == segment.IsDoubleUnderline &&
                    lastExistingSegment.IsStrikethrough == segment.IsStrikethrough &&
                    lastExistingSegment.IsOverline == segment.IsOverline &&
                    ReferenceEquals(lastExistingSegment.UnderlineColor, segment.UnderlineColor))
                {
                    // Merge by extending the last segment
                    lastExistingSegment.Length += segment.Length;
                }
                else
                {
                    _formattingSegments.Add(segment);
                }
            }
            
            // Limit total number of segments to prevent performance issues
            const int maxSegments = 10000;
            if (_formattingSegments.Count > maxSegments)
            {
                int toRemove = _formattingSegments.Count - maxSegments;
                _formattingSegments.RemoveRange(0, toRemove);
            }

            s.Clear();
            _outputElementListPool.Return(s);
        });
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
    private List<FormattedSegment> _formattingSegments = new();
    private readonly LogForwarder _logForwarder;

    public CancellationTokenSource GracefulCancellation { get; set; }
    public CancellationTokenSource KillCancellation { get; set; }
    public Dictionary<string, string?> EnvironmentVariables { get; set; }

    public TextDocument RichOutput { get; set; } = new();
    public List<FormattedSegment> FormattingSegments => _formattingSegments;
   
    private bool _followOutput = true;
    public bool FollowOutput
    {
        get => _followOutput;
        set => this.RaiseAndSetIfChanged(ref _followOutput, value);
    }
}

public class FormattedSegment
{
    public int StartOffset { get; set; }
    public int Length { get; set; }
    public IBrush? Foreground { get; set; }
    public IBrush? Background { get; set; }
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsUnderline { get; set; }
    public bool IsDoubleUnderline { get; set; }
    public bool IsStrikethrough { get; set; }
    public bool IsOverline { get; set; }
    public IBrush? UnderlineColor { get; set; }
    public bool IsLink { get; set; }
    public string? LinkUrl { get; set; }
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
