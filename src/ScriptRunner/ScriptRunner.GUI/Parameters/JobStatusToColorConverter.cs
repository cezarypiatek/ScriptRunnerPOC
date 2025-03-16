using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI;

public class JobStatusToColorConverter:  IValueConverter
{

    public static JobStatusToColorConverter Instance { get; } = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RunningJobStatus status)
        {
            return status switch
            {
                RunningJobStatus.NotStarted => new SolidColorBrush(Colors.Black),
                RunningJobStatus.Running => new SolidColorBrush(Colors.LightGreen),
                RunningJobStatus.Cancelled => new SolidColorBrush(Colors.Yellow),
                RunningJobStatus.Failed => new SolidColorBrush(Colors.Red),
                RunningJobStatus.Finished => new SolidColorBrush(Colors.Gray),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}