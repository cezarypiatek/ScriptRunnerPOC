using System;
using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class TimePickerControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        var selectedTime = ((ScriptRunner.GUI.Views.Controls.TimePickerInput)Control).SelectedTime;
        if (string.IsNullOrWhiteSpace(Format) == false && selectedTime is {} value)
        {
            return value.ToString(Format);
        }
        return selectedTime?.ToString() ?? string.Empty;
    }

    public bool IsNotEmpty() => ((ScriptRunner.GUI.Views.Controls.TimePickerInput)Control).SelectedTime != null;

    public void SetValueFromString(string value)
    {
        if (TryParseValue(value, Format, out var ts))
            ((ScriptRunner.GUI.Views.Controls.TimePickerInput)Control).SelectedTime = ts;
    }

    internal static bool TryParseValue(string value, string? format, out TimeSpan time)
    {
        return string.IsNullOrWhiteSpace(format)
            ? TimeSpan.TryParse(value, System.Globalization.CultureInfo.CurrentCulture, out time)
            : TimeSpan.TryParseExact(value, format, System.Globalization.CultureInfo.CurrentCulture, out time);
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public bool Required { get; set; }

    public string? Format { get; set; }
}
