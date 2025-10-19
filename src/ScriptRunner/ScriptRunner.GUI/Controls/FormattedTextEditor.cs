using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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
