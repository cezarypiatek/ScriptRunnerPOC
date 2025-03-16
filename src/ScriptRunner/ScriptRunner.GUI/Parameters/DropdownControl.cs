using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class DropdownControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((ComboBox)Control).SelectedItem?.ToString();
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}