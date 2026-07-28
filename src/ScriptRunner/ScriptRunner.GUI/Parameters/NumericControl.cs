using System;
using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class NumericControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((NumericUpDown)Control).Value?.ToString(System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
    }

    public bool IsNotEmpty() => ((NumericUpDown)Control).Value.HasValue;

    public void SetValueFromString(string value)
    {
        if (TryParseValue(value, out var num))
            ((NumericUpDown)Control).Value = num;
    }

    internal static bool TryParseValue(string? value, out decimal number)
    {
        return decimal.TryParse(
                   value,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out number)
               || decimal.TryParse(
                   value,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.CurrentCulture,
                   out number);
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public bool Required { get; set; }
}
