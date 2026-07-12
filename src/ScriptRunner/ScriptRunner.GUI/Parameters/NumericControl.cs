using System;
using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class NumericControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((NumericUpDown)Control).Text;
    }

    public bool IsNotEmpty() => !string.IsNullOrWhiteSpace(((NumericUpDown)Control).Text);

    public void SetValueFromString(string value)
    {
        if (decimal.TryParse(value, out var num))
            ((NumericUpDown)Control).Value = num;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public bool Required { get; set; }
}
