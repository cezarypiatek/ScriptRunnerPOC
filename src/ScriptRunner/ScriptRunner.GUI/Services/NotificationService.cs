using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Labs.Notifications;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Services;

/// <summary>
/// Cross-platform system notification service using Avalonia.Labs.Notifications
/// </summary>
public class NotificationService : INotificationService
{
    private Window? _mainWindow;

    /// <inheritdoc/>
    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
    }

    /// <inheritdoc/>
    public Task ShowJobCompletedAsync(string jobTitle, RunningJobStatus status, TimeSpan elapsed)
    {
        // Only show notification if window is not active/focused
        if (_mainWindow?.IsActive == true)
        {
            return Task.CompletedTask; // Window is active, don't show notification
        }

        try
        {
            var manager = NativeNotificationManager.Current;
            if (manager == null)
            {
                return Task.CompletedTask; // Notification manager not initialized
            }

            var title = GetNotificationTitle(status);
            var message = $"{jobTitle}\nCompleted in {FormatElapsedTime(elapsed)}";

            var notification = manager.CreateNotification(null);
            if (notification != null)
            {
                notification.Title = title;
                notification.Message = message;
                notification.Show();
            }
        }
        catch (Exception)
        {
            // Silently catch any exceptions from the notification system
            // to prevent crashes (especially on macOS where there are known issues)
            // See: https://github.com/AvaloniaUI/Avalonia.Labs/issues/114
        }

        return Task.CompletedTask;
    }

    private static string GetNotificationTitle(RunningJobStatus status)
    {
        return status switch
        {
            RunningJobStatus.Finished => "🎉 Job Completed Successfully",
            RunningJobStatus.Failed => "🩻 Job Failed",
            RunningJobStatus.Cancelled => "Job Cancelled",
            _ => "Job Status Update"
        };
    }

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 1)
        {
            return $"{elapsed.TotalMilliseconds:F0}ms";
        }
        else if (elapsed.TotalMinutes < 1)
        {
            return $"{elapsed.TotalSeconds:F1}s";
        }
        else if (elapsed.TotalHours < 1)
        {
            return $"{elapsed.Minutes}m {elapsed.Seconds}s";
        }
        else
        {
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
        }
    }
}
