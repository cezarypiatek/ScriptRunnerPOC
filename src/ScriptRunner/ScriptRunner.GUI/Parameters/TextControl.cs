using Avalonia.Controls;
using AvaloniaEdit;

namespace ScriptRunner.GUI;

public class TextControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return Control switch
        {
            TextBox textBox => textBox.Text,
            TextEditor textEditor => textEditor.Text,
            _ => ((TextBox)Control).Text
        };
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}