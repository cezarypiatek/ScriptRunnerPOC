using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using AvaloniaEdit;
using Projektanker.Icons.Avalonia;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

internal sealed class MultilineEditorHost : Border
{
    private readonly Control _editor;
    private readonly Visual _resizeCoordinateSpace;
    private bool _isResizing;
    private Point _lastPointerPosition;

    public MultilineEditorHost(Control editor, Visual resizeCoordinateSpace)
    {
        _editor = editor;
        _resizeCoordinateSpace = resizeCoordinateSpace;

        Classes.Add("multilineEditorFrame");
        Child = CreateLayout();
    }

    private Control CreateLayout()
    {
        if (_editor is TextEditor textEditor)
        {
            textEditor.BorderThickness = new Thickness(0);
            textEditor.CornerRadius = new CornerRadius(0);
        }

        var toolbar = CreateToolbar();
        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto")
        };
        layout.Children.Add(_editor);
        Grid.SetRow(toolbar, 1);
        layout.Children.Add(toolbar);

        return layout;
    }

    private Control CreateToolbar()
    {
        var expandButton = new Button
        {
            Classes = { "multilineEditorToolbarButton" },
            Content = new Icon
            {
                Value = "fas fa-expand",
                FontSize = 10
            }
        };
        ToolTip.SetTip(expandButton, "Open in larger editor");
        expandButton.Click += OpenEditor;

        var resizeHandle = new Border
        {
            Classes = { "multilineEditorResizeGrip" },
            Cursor = new Cursor(StandardCursorType.BottomRightCorner),
            Child = new Icon
            {
                Value = "fas fa-signal",
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        ToolTip.SetTip(resizeHandle, "Drag to resize editor");
        resizeHandle.PointerPressed += StartResize;
        resizeHandle.PointerMoved += ResizeEditor;
        resizeHandle.PointerReleased += StopResize;
        resizeHandle.PointerCaptureLost += (_, _) => _isResizing = false;

        var toolbarContent = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto")
        };
        Grid.SetColumn(expandButton, 1);
        toolbarContent.Children.Add(expandButton);
        Grid.SetColumn(resizeHandle, 2);
        toolbarContent.Children.Add(resizeHandle);

        return new Border
        {
            Classes = { "multilineEditorToolbar" },
            Child = toolbarContent
        };
    }

    private async void OpenEditor(object? sender, RoutedEventArgs e)
    {
        var overlay = new TextEditorOverlay();
        overlay.SetEditorControl(_editor);

        if (TopLevel.GetTopLevel(_editor) is Window parentWindow)
        {
            await overlay.ShowDialog(parentWindow);
        }
        else
        {
            overlay.Show();
        }
    }

    private void StartResize(object? sender, PointerPressedEventArgs e)
    {
        _isResizing = true;
        _lastPointerPosition = e.GetPosition(_resizeCoordinateSpace);
        e.Pointer.Capture((IInputElement?)sender);
        e.Handled = true;
    }

    private void ResizeEditor(object? sender, PointerEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        var currentPosition = e.GetPosition(_resizeCoordinateSpace);
        var delta = currentPosition - _lastPointerPosition;

        _editor.Height = Math.Max(_editor.MinHeight, _editor.Height + delta.Y);

        var currentWidth = double.IsNaN(_editor.Width) ? _editor.Bounds.Width : _editor.Width;
        _editor.Width = Math.Max(100, currentWidth + delta.X);

        _lastPointerPosition = currentPosition;
    }

    private void StopResize(object? sender, PointerReleasedEventArgs e)
    {
        _isResizing = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}
