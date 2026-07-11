using System.Collections.Generic;
using Avalonia.Controls;

using Avalonia.Layout;

namespace ScriptRunner.GUI;

public class ParamsPanel
{
    public Panel Panel { get; set; }

    public IEnumerable<IControlRecord> ControlRecords { get; set; }

    /// <summary>
    /// Maps parameter name → the wrapping Border around that parameter's row.
    /// Used to apply/clear the orange MCP-modified highlight.
    /// </summary>
    public Dictionary<string, Border> ParameterContainers { get; set; } = new();
}

public class ParameterFitHost : Grid
{
    private readonly ScrollViewer _scrollViewer;
    private readonly Viewbox _viewbox;
    private readonly Control _content;

    public ParameterFitHost(Control content)
    {
        _content = content;
        _scrollViewer = new ScrollViewer
        {
            Content = content,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        _viewbox = new Viewbox
        {
            IsVisible = false,
            Stretch = Avalonia.Media.Stretch.Uniform,
            StretchDirection = Avalonia.Media.StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        Children.Add(_scrollViewer);
        Children.Add(_viewbox);
    }

    public void SetFitToArea(bool fitToArea)
    {
        if (fitToArea)
        {
            _scrollViewer.Content = null;
            _viewbox.Child = _content;
        }
        else
        {
            _viewbox.Child = null;
            _scrollViewer.Content = _content;
        }

        _scrollViewer.IsVisible = !fitToArea;
        _viewbox.IsVisible = fitToArea;
    }
}
