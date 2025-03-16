using Avalonia.Controls;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class DropdownControl : IControlRecord
{
    public Control Control { get; set; }
    public Control InputControl { get; set; }

    public string GetFormattedValue()
    {
        return InputControl switch
        {
            ComboBox cb => cb.SelectedItem?.ToString(),
            SearchableComboBox acb => acb.SelectedItem,
            _ => ""
        } ?? string.Empty;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}