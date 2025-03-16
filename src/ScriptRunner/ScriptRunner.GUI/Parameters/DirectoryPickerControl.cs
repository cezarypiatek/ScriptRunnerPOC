using Avalonia.Controls;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class DirectoryPickerControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((DirectoryPicker)Control).DirPath;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}