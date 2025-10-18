using System;

namespace ScriptRunner.GUI.ViewModels;

/// <summary>
/// Represents a date group in the date picker with count of items
/// </summary>
public class DateGroupInfo
{
    public DateTime Date { get; }
    public string DateDisplay { get; }
    public int Count { get; set; }

    public DateGroupInfo(DateTime date, int count)
    {
        Date = date.Date;
        Count = count;
        DateDisplay = GetDateDisplay(date);
    }

    private string GetDateDisplay(DateTime date)
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        if (date.Date == today)
            return "Today";
        else if (date.Date == yesterday)
            return "Yesterday";
        else if (date.Date > today.AddDays(-7))
            return date.ToString("dddd, MMMM dd"); // Day of week with date
        else
            return date.ToString("MMMM dd, yyyy");
    }
}

