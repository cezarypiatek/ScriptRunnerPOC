using System.Globalization;
using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class DatePickerControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        var selectedDateTime =  Control switch
        {
            DatePicker dp => dp.SelectedDate?.DateTime,
            CalendarDatePicker cdp => cdp.SelectedDate?.Date,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(Format) == false && selectedDateTime is {} value)
        {
            return value.ToString(Format, Culture);
        }
        return selectedDateTime?.ToString() ?? string.Empty;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }

    public string? Format { get; set; }
    public CultureInfo Culture { get; set; }
}