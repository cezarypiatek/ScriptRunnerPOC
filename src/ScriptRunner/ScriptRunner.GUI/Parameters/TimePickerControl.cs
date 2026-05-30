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

    public void SetValueFromString(string value)
    {
        if (TimeSpan.TryParse(value, out var ts))
            ((ScriptRunner.GUI.Views.Controls.TimePickerInput)Control).SelectedTime = ts;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }

    public string? Format { get; set; }
}
