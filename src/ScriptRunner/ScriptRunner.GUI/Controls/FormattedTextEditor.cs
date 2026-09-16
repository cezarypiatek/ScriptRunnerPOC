using System;
using System.Diagnostics;
using System.Linq;
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
        FontFamily = new FontFamily(
            "Cascadia Mono, Cascadia Code, Consolas, Segoe UI Symbol, Segoe UI Emoji, " +
            "Yu Gothic UI, Malgun Gothic, Noto Sans Mono CJK SC, Noto Sans Mono, Monospace");
        Options.AllowScrollBelowDocument = false;
        Options.RequireControlModifierForHyperlinkClick = false;
        Options.EnableHyperlinks = false;
        Padding = new Thickness(15);
        TextArea.TextView.LinkTextForegroundBrush = Brushes.LightBlue;
        TextArea.TextView.ElementGenerators.Add(new OutputLinkElementGenerator(() => ViewModel));
        
        // Add context menu
        ContextMenu = CreateContextMenu();
        
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

    private ContextMenu CreateContextMenu()
    {
        var contextMenu = new ContextMenu();
        
        var copySelectedItem = new MenuItem
        {
            Header = "Copy Selected"
        };
        copySelectedItem.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(SelectedText))
            {
                CopyToClipboard(SelectedText);
            }
        };
        
        var copyAllItem = new MenuItem
        {
            Header = "Copy All"
        };
        copyAllItem.Click += (_, _) =>
        {
            if (Document != null)
            {
                CopyToClipboard(Document.Text);
            }
        };
        
        var selectAllItem = new MenuItem
        {
            Header = "Select All"
        };
        selectAllItem.Click += (_, _) =>
        {
            if (Document != null)
            {
                SelectionStart = 0;
                SelectionLength = Document.TextLength;
            }
        };
        
        var searchItem = new MenuItem
        {
            Header = "Search (Ctrl+F)"
        };
        searchItem.Click += (_, _) =>
        {
            // Trigger the built-in search functionality
            var searchPanel = AvaloniaEdit.Search.SearchPanel.Install(this);
            searchPanel?.Open();
        };
        
        contextMenu.Items.Add(copySelectedItem);
        contextMenu.Items.Add(copyAllItem);
        contextMenu.Items.Add(selectAllItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(searchItem);
        
        // Update menu items based on selection when opening
        contextMenu.Opening += (_, _) =>
        {
            copySelectedItem.IsEnabled = !string.IsNullOrEmpty(SelectedText);
            copyAllItem.IsEnabled = Document != null && !string.IsNullOrEmpty(Document.Text);
        };
        
        return contextMenu;
    }

    private async void CopyToClipboard(string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
        }
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

                // Apply text decorations (underline and/or strikethrough)
                if (segment.IsUnderline || segment.IsStrikethrough || segment.IsOverline)
                {
                    var decorations = new TextDecorationCollection();
                    
                    if (segment.IsUnderline)
                    {
                        decorations.Add(new TextDecoration
                        {
                            Location = TextDecorationLocation.Underline,
                            Stroke = segment.UnderlineColor,
                            StrokeThickness = segment.IsDoubleUnderline ? 2 : 1
                        });
                    }
                    
                    if (segment.IsStrikethrough)
                    {
                        decorations.Add(new TextDecoration { Location = TextDecorationLocation.Strikethrough });
                    }

                    if (segment.IsOverline)
                    {
                        decorations.Add(new TextDecoration { Location = TextDecorationLocation.Overline });
                    }
                    
                    element.TextRunProperties.SetTextDecorations(decorations);
                }
            });
        }
    }
}



/// <summary>
/// Makes the link ranges detected while parsing output clickable.
/// </summary>
public class OutputLinkElementGenerator : VisualLineElementGenerator
{
    private readonly Func<RunningJobViewModel?> _viewModelAccessor;
    public bool RequireControlModifierForClick { get; set; }

    public OutputLinkElementGenerator(Func<RunningJobViewModel?> viewModelAccessor)
    {
        _viewModelAccessor = viewModelAccessor;
        RequireControlModifierForClick = false;
    }

    private FormattedSegment? GetSegment(int startOffset, out int matchOffset)
    {
        var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
        var segment = _viewModelAccessor()?.FormattingSegments
            .FirstOrDefault(item => item.IsLink &&
                                    item.StartOffset + item.Length > startOffset &&
                                    item.StartOffset < endOffset);

        if (segment is null)
        {
            matchOffset = -1;
            return null;
        }

        matchOffset = Math.Max(segment.StartOffset, startOffset);
        return segment;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        GetSegment(startOffset, out var matchOffset);
        return matchOffset;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        var segment = GetSegment(offset, out var matchOffset);
        if (segment is not null && matchOffset == offset && !string.IsNullOrEmpty(segment.LinkUrl))
        {
            var segmentEnd = segment.StartOffset + segment.Length;
            var availableLength = CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset;
            var length = Math.Min(segmentEnd - offset, availableLength);
            if (length > 0)
            {
                return new OutputLinkText(CurrentContext.VisualLine, length)
                {
                    Target = segment.LinkUrl,
                    RequireControlModifierForClick = RequireControlModifierForClick
                };
            }
        }
        return null;
    }
}

/// <summary>
/// Visual line element representing a clickable URL or local path.
/// </summary>
public class OutputLinkText : VisualLineText
{
    public string Target { get; set; } = string.Empty;
    public bool RequireControlModifierForClick { get; set; }

    public OutputLinkText(VisualLine parentVisualLine, int length) 
        : base(parentVisualLine, length)
    {
        RequireControlModifierForClick = true;
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        return base.CreateTextRun(startVisualColumn, context);
    }

    protected virtual bool LinkIsClickable(KeyModifiers modifiers)
    {
        if (string.IsNullOrEmpty(Target))
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
            OpenTarget(Target);
            e.Handled = true;
        }
    }

    private static void OpenTarget(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open link target: {ex.Message}");
        }
    }

    protected override VisualLineText CreateInstance(int length)
    {
        return new OutputLinkText(ParentVisualLine, length)
        {
            Target = Target,
            RequireControlModifierForClick = RequireControlModifierForClick
        };
    }
}
