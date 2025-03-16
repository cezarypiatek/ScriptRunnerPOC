using Avalonia.Controls;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class FilePickerControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((FilePicker)Control).FilePath;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}