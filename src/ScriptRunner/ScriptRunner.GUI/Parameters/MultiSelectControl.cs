using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI;

public class MultiSelectControl : IControlRecord
{
    public Control Control { get; set; }
    public IEnumerable<DropdownOption>? Options { get; set; }

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

    public void SetValueFromString(string value)
    {
        var lb = (ListBox)Control;
        lb.SelectedItems?.Clear();
        
        var parts = TryDeserialized(value) switch
        {
            (true, var elements) => elements ?? value.Split(Delimiter),
            _ => value.Split(Delimiter)
            
        };
        foreach (var part in parts)
        {
            var match = Options?.FirstOrDefault(o => o.Value == part.Trim()) as object
                        ?? lb.Items.OfType<object>().FirstOrDefault(i => i?.ToString() == part.Trim());
            if (match != null)
            {
                lb.SelectedItems?.Add(match);
            }
        }
    }

    private static (bool, string[]?) TryDeserialized(string value)
    {
        try
        {
            return (true,  JsonSerializer.Deserialize<string[]>(value));
        }
        catch (Exception e)
        {
            return (false, Array.Empty<string>());
        }
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public string Delimiter { get; set; }

}
