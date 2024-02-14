using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ScriptRunner.GUI.ViewModels;

public class TroubleShootingSeverityToBrushConverter : IValueConverter
{
    public static readonly TroubleShootingSeverityToBrushConverter Instance = new TroubleShootingSeverityToBrushConverter();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TroubleShootingSeverity severity)
        {
            return severity switch
            {
                TroubleShootingSeverity.Error => Brushes.Red,
                TroubleShootingSeverity.Warning => Brushes.Orange,
                TroubleShootingSeverity.Info => Brushes.DodgerBlue,
                TroubleShootingSeverity.Success => Brushes.Green,
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Conversion back is not supported or necessary in this scenario
        throw new NotImplementedException();
    }
}