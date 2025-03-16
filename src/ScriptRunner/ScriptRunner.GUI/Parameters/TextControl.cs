using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class TextControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((TextBox)Control).Text;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}