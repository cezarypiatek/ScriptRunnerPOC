using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ScriptRunner.GUI.Converters;

public class CategoryToColorConverter : IValueConverter
{
    private static readonly Color[] PredefinedColors = new[]
    {
        Color.FromRgb(70, 100, 180),  // Darker Blue
        Color.FromRgb(200, 90, 70),   // Darker Coral
        Color.FromRgb(80, 150, 80),   // Darker Green
        Color.FromRgb(150, 100, 150), // Darker Plum
        Color.FromRgb(180, 140, 0),   // Darker Gold
        Color.FromRgb(85, 130, 160),  // Darker Sky Blue
        Color.FromRgb(180, 100, 120), // Darker Pink
        Color.FromRgb(90, 160, 90),   // Darker Pale Green
        Color.FromRgb(180, 100, 80),  // Darker Salmon
        Color.FromRgb(110, 130, 150), // Darker Steel Blue
        Color.FromRgb(160, 80, 160),  // Darker Violet
        Color.FromRgb(180, 130, 100), // Darker Peach
        Color.FromRgb(100, 140, 170), // Darker Light Blue
        Color.FromRgb(170, 80, 80),   // Darker Coral Red
        Color.FromRgb(120, 140, 70),  // Olive Green
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string category)
        {
            // Generate deterministic hash from category name
            int hash = GetDeterministicHashCode(category);
            int colorIndex = Math.Abs(hash) % PredefinedColors.Length;
            return new SolidColorBrush(PredefinedColors[colorIndex]);
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1 || str[i + 1] == '\0')
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}
