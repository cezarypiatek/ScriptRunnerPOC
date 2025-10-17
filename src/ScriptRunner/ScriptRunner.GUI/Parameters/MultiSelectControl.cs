using System.Collections.Generic;
using Avalonia.Controls;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI;

public class MultiSelectControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        var selectedItems = ((ListBox)Control).SelectedItems;
        var copy = new List<string>();
        foreach (var item in selectedItems)
        {
            if (item is DropdownOption option)
            {
                copy.Add(option.Value);
            }
            else if (item?.ToString() is { } nonNullItem)
            {
                copy.Add(nonNullItem);
            }
        }

        return string.Join(Delimiter, copy);
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public string Delimiter { get; set; }

}