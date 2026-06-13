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

    public void SetValueFromString(string value)
    {
        if (DateTime.TryParse(value, Culture, DateTimeStyles.None, out var dt))
        {
            DateControl.SelectedDate = dt.Date;
            TimeControl.SelectedTime = dt.TimeOfDay;
        }
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}
