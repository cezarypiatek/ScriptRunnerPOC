using System;

namespace ScriptRunner.GUI.ViewModels;

/// <summary>
/// Base class for items in the execution log list (both actions and date headers)
/// </summary>
public abstract class ExecutionLogItemBase
{
}

/// <summary>
/// Represents a date header divider in the execution log
/// </summary>
public class ExecutionLogDateHeader : ExecutionLogItemBase
{
    public DateTime Date { get; }
    public string DateDisplay { get; }
    
    // Property to control highlight animation
    public bool IsHighlighted { get; set; }

    public ExecutionLogDateHeader(DateTime date)
    {
        Date = date.Date;
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
            return date.ToString("dddd"); // Day of week
        else
            return date.ToString("MMMM dd, yyyy");
    }
}

/// <summary>
/// Wrapper for ExecutionLogAction to fit in the grouped list
/// </summary>
public class ExecutionLogItemAction : ExecutionLogItemBase
{
    public ExecutionLogAction Action { get; }

    public ExecutionLogItemAction(ExecutionLogAction action)
    {
        Action = action;
    }
}
