using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class DropdownControl : IControlRecord
{
    public Control Control { get; set; }
    public Control InputControl { get; set; }
    public ObservableCollection<DropdownOption> DropdownOptions { get; set; }

    public string GetFormattedValue()
    {
        var selectedItem = InputControl switch
        {
            ComboBox cb => cb.SelectedItem,
            SearchableComboBox acb => acb.SelectedItem,
            _ => null
        };

        if (selectedItem is DropdownOption option)
        {
            return option.Value;
        }
        
        // For SearchableComboBox, we need to map the label back to value
        if (selectedItem is string label && DropdownOptions != null)
        {
            var matchingOption = DropdownOptions.FirstOrDefault(opt => opt.Label == label);
            if (matchingOption != null)
            {
                return matchingOption.Value;
            }
        }

        return selectedItem?.ToString() ?? string.Empty;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}