using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ScriptRunner.GUI.Converters;

public class IntensityToColorConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is int intensity)
        {
            return intensity switch
            {
                0 => Color.Parse("#161b22"),
                1 => Color.Parse("#0e4429"),
                2 => Color.Parse("#006d32"),
                3 => Color.Parse("#26a641"),
                4 => Color.Parse("#39d353"),
                _ => Color.Parse("#161b22")
            };
        }
        return Color.Parse("#161b22");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IntensityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intensity && parameter is string paramStr && int.TryParse(paramStr, out var targetIntensity))
        {
            return intensity == targetIntensity;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class WeekToPositionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int weekIndex)
        {
            return weekIndex * 14; // 12px width + 2px margin
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DayToPositionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int dayOfWeek)
        {
            return dayOfWeek * 14; // 12px height + 2px margin
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MonthLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            var parts = str.Split('|');
            return parts.Length > 0 ? parts[0] : "";
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MonthWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            var parts = str.Split('|');
            if (parts.Length > 1 && int.TryParse(parts[1], out var weekCount))
            {
                // Each week is 14px wide (12px cell + 2px margin)
                return weekCount * 14;
            }
        }
        return 56; // Default to 4 weeks
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MonthPositionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            var parts = str.Split('|');
            if (parts.Length > 1 && int.TryParse(parts[1], out var weekIndex))
            {
                return weekIndex * 14; // 12px width + 2px margin
            }
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IndexToRankConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return (index + 1).ToString();
        }
        return "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
