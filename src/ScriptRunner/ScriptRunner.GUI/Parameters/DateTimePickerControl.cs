using System;
using System.Globalization;
using Avalonia.Controls;
using ScriptRunner.GUI.Views.Controls;

namespace ScriptRunner.GUI;

public class DateTimePickerControl : IControlRecord
{
    public Control Control { get; set; }
    public CalendarDatePicker DateControl { get; set; }
    public TimePickerInput TimeControl { get; set; }

    public string? Format { get; set; }
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    public string GetFormattedValue()
    {
        var date = DateControl.SelectedDate?.Date;
        var time = TimeControl.SelectedTime;

        if (date == null && time == null)
            return string.Empty;

        var dt = (date ?? DateTime.Today).Add(time ?? TimeSpan.Zero);
        var fmt = string.IsNullOrWhiteSpace(Format) ? "yyyy-MM-dd HH:mm" : Format;
        return dt.ToString(fmt, Culture);
    }

    public bool IsNotEmpty() => DateControl.SelectedDate != null || TimeControl.SelectedTime != null;

    public void SetValueFromString(string value)
    {
        if (TryParseValue(value, Format, Culture, out var dt))
        {
            DateControl.SelectedDate = dt.Date;
            TimeControl.SelectedTime = dt.TimeOfDay;
        }
    }

    internal static bool TryParseValue(string value, string? format, CultureInfo culture, out DateTime dateTime)
    {
        return string.IsNullOrWhiteSpace(format)
            ? DateTime.TryParse(value, culture, DateTimeStyles.None, out dateTime)
            : DateTime.TryParseExact(value, format, culture, DateTimeStyles.None, out dateTime);
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public bool Required { get; set; }
}
