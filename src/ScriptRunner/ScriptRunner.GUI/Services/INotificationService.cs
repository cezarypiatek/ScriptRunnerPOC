using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Services;

/// <summary>
/// Service for showing system notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sets the main window reference to check if it's active
    /// </summary>
    /// <param name="window">The main application window</param>
    void SetMainWindow(Window window);
    
    /// <summary>
    /// Shows a notification when a job completes (only if window is not active)
    /// </summary>
    /// <param name="jobTitle">Title of the job that completed</param>
    /// <param name="status">Final status of the job</param>
    /// <param name="elapsed">Time elapsed for job execution</param>
    Task ShowJobCompletedAsync(string jobTitle, RunningJobStatus status, TimeSpan elapsed);
}
