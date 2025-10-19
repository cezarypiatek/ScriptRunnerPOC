using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Controls;

public class FormattedTextEditor : TextEditor
{
    public static readonly StyledProperty<RunningJobViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<FormattedTextEditor, RunningJobViewModel?>(nameof(ViewModel));

    public RunningJobViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    protected override Type StyleKeyOverride { get; } = typeof(TextEditor);

    private FormattedTextColorizer? _colorizer;

    public event EventHandler<ScrollChangedEventArgs>? ScrollChanged;

    public FormattedTextEditor()
    {
        IsReadOnly = true;
        ShowLineNumbers = false;
        WordWrap = true;
        Background = new SolidColorBrush(Color.FromRgb(30,30,30));
        BorderBrush = new SolidColorBrush(Color.FromRgb(62,62,54));
        BorderThickness = new Thickness(1);
        FontFamily = new FontFamily("Consolas");
        Options.AllowScrollBelowDocument = false;
        Options.RequireControlModifierForHyperlinkClick = false;
        Padding = new Thickness(15);
        TextArea.TextView.LinkTextForegroundBrush = Brushes.LightBlue;
        TextArea.TextView.ElementGenerators.Add(new FilePathElementGenerator());
        // Subscribe to scroll changes
        this.Loaded += (_, _) =>
        {
            var scrollViewer = this.GetVisualDescendants()
                .OfType<ScrollViewer>()
                .FirstOrDefault();
            
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += OnScrollChanged;
            }
        };
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        ScrollChanged?.Invoke(this, e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ViewModelProperty)
        {
            if (_colorizer != null)
            {
                TextArea.TextView.LineTransformers.Remove(_colorizer);
            }

            if (ViewModel != null)
            {
                Document = ViewModel.RichOutput;
                _colorizer = new FormattedTextColorizer(ViewModel);
                TextArea.TextView.LineTransformers.Add(_colorizer);
            }
        }
    }
}

public class FormattedTextColorizer : DocumentColorizingTransformer
{
    private readonly RunningJobViewModel _viewModel;

    public FormattedTextColorizer(RunningJobViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        int lineStartOffset = line.Offset;
        int lineEndOffset = line.EndOffset;

        var segments = _viewModel.FormattingSegments;
        
        foreach (var segment in segments)
        {
            // Skip segments completely before this line
            if (segment.StartOffset + segment.Length <= lineStartOffset)
                continue;
            
            // Stop if segment is completely after this line
            if (segment.StartOffset >= lineEndOffset)
                break;

            int segmentStart = Math.Max(segment.StartOffset, lineStartOffset);
            int segmentEnd = Math.Min(segment.StartOffset + segment.Length, lineEndOffset);

            if (segmentStart >= segmentEnd)
                continue;

            ChangeLinePart(segmentStart, segmentEnd, element =>
            {
                if (segment.Foreground != null)
                {
                    element.TextRunProperties.SetForegroundBrush(segment.Foreground);
                }

                if (segment.Background != null && !segment.Background.Equals(Brushes.Transparent))
                {
                    element.TextRunProperties.SetBackgroundBrush(segment.Background);
                }

                var typeface = element.TextRunProperties.Typeface;
                var newTypeface = new Typeface(
                    typeface.FontFamily,
                    segment.IsItalic ? FontStyle.Italic : FontStyle.Normal,
                    segment.IsBold ? FontWeight.Bold : FontWeight.Normal
                );
                element.TextRunProperties.SetTypeface(newTypeface);

                if (segment.IsUnderline)
                {
                    element.TextRunProperties.SetTextDecorations(new TextDecorationCollection
                    {
                        new TextDecoration { Location = TextDecorationLocation.Underline }
                    });
                }
            });
        }
    }
}



/// <summary>
/// Detects file and directory paths and makes them clickable.
/// </summary>
public class FilePathElementGenerator : VisualLineElementGenerator
{
    // Windows paths:
    // - Starts with drive letter (C:\) or UNC path (\\server\)
    // - Can contain spaces and most characters except: < > : " | ? * [ ] and control chars
    // - Terminates at: whitespace, quotes, brackets, or line break
    // - For files: must end with extension (.txt, .cs, etc.)
    // - For dirs: can end with \ or directory name
    private static readonly Regex WindowsPathRegex = new Regex(
        @"(?:[a-zA-Z]:\\|\\\\[^\\]+\\[^\\]+\\)" +  // Drive (C:\) or UNC (\\server\share\)
        @"(?:[^<>:""|?*\[\]\r\n]+\\)*" +            // Intermediate directories (can have spaces)
        @"(?:[^<>:""|?*\[\]\r\n\\]+(?:\.[a-zA-Z0-9]+)?|[^<>:""|?*\[\]\r\n\\]+\\)", // Final file with extension or directory
        RegexOptions.Compiled);
    
    // Unix paths:
    // - Starts with / or ~/
    // - Can contain spaces in directory/file names
    // - Terminates at: whitespace, quotes, brackets, or special chars
    private static readonly Regex UnixPathRegex = new Regex(
        @"(?:~/|/)" +                               // Root or home
        @"(?:[^<>:""\\\|?*\[\]\s\n]+/)*" +          // Directories (terminated by /)
        @"[^<>:""\\\|?*\[\]\s\n]+",                 // Final file or directory name
        RegexOptions.Compiled);
    public bool RequireControlModifierForClick { get; set; }

    public FilePathElementGenerator()
    {
        RequireControlModifierForClick = false;
    }

    private Match GetMatch(int startOffset, out int matchOffset)
    {
        var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
        var relevantText = CurrentContext.GetText(startOffset, endOffset - startOffset);
        
        // Try Windows paths first
        var match = WindowsPathRegex.Match(relevantText.Text, relevantText.Offset, relevantText.Count);
        
        // If no Windows path found, try Unix paths
        if (!match.Success)
        {
            match = UnixPathRegex.Match(relevantText.Text, relevantText.Offset, relevantText.Count);
        }
        
        matchOffset = match.Success ? match.Index - relevantText.Offset + startOffset : -1;
        return match;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        GetMatch(startOffset, out var matchOffset);
        return matchOffset;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        var match = GetMatch(offset, out var matchOffset);
        if (match.Success && matchOffset == offset)
        {
            var path = match.Value;
            
            // Validate that the path exists
            if (File.Exists(path) || Directory.Exists(path))
            {
                return new FilePathLinkText(CurrentContext.VisualLine, match.Length)
                {
                    Path = path,
                    RequireControlModifierForClick = RequireControlModifierForClick
                };
            }
        }
        return null;
    }
}

/// <summary>
/// Visual line element representing a clickable file path.
/// </summary>
public class FilePathLinkText : VisualLineText
{
    public string Path { get; set; }
    public bool RequireControlModifierForClick { get; set; }

    public FilePathLinkText(VisualLine parentVisualLine, int length) 
        : base(parentVisualLine, length)
    {
        RequireControlModifierForClick = true;
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        // Apply link styling
        this.TextRunProperties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
        this.TextRunProperties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
        if (context.TextView.LinkTextUnderline)
            this.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
        return base.CreateTextRun(startVisualColumn, context);
    }

    protected virtual bool LinkIsClickable(KeyModifiers modifiers)
    {
        if (string.IsNullOrEmpty(Path))
            return false;
        if (RequireControlModifierForClick)
            return modifiers.HasFlag(KeyModifiers.Control);
        return true;
    }

    protected override void OnQueryCursor(PointerEventArgs e)
    {
        if (LinkIsClickable(e.KeyModifiers))
        {
            if (e.Source is InputElement inputElement)
            {
                inputElement.Cursor = new Cursor(StandardCursorType.Hand);
            }
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.Handled && LinkIsClickable(e.KeyModifiers))
        {
            OpenPath(Path);
            e.Handled = true;
        }
    }

    private static void OpenPath(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                // Open file with default application
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            else if (Directory.Exists(path))
            {
                // Open directory in file explorer
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            // Handle errors (log or show notification)
            System.Diagnostics.Debug.WriteLine($"Failed to open path: {ex.Message}");
        }
    }

    protected override VisualLineText CreateInstance(int length)
    {
        return new FilePathLinkText(ParentVisualLine, length)
        {
            Path = Path,
            RequireControlModifierForClick = RequireControlModifierForClick
        };
    }
}