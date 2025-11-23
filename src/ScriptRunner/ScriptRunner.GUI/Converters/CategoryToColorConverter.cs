using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ScriptRunner.GUI.Converters;

public class CategoryToColorConverter : IValueConverter
{
    private static readonly Color[] PredefinedColors = new[]
    {
        // Blues & Cyans (8 colors)
        Color.FromRgb(25, 95, 145),    // Dark Steel Blue
        Color.FromRgb(0, 105, 200),    // Strong Blue
        Color.FromRgb(0, 130, 200),    // Deep Blue
        Color.FromRgb(0, 140, 140),    // Deep Teal
        Color.FromRgb(0, 150, 136),    // Dark Turquoise
        Color.FromRgb(30, 80, 180),    // Royal Blue
        Color.FromRgb(0, 85, 140),     // Navy Blue
        Color.FromRgb(0, 120, 150),    // Ocean Blue
        
        // Greens (8 colors)
        Color.FromRgb(30, 130, 76),    // Dark Sea Green
        Color.FromRgb(22, 160, 80),    // Deep Emerald
        Color.FromRgb(65, 165, 65),    // Forest Green
        Color.FromRgb(50, 120, 50),    // Dark Green
        Color.FromRgb(100, 140, 30),   // Olive Green
        Color.FromRgb(0, 135, 60),     // Green
        Color.FromRgb(40, 105, 40),    // Hunter Green
        Color.FromRgb(85, 150, 0),     // Lime Green
        
        // Purples & Magentas (8 colors)
        Color.FromRgb(100, 65, 165),   // Deep Purple
        Color.FromRgb(90, 24, 154),    // Dark Violet
        Color.FromRgb(130, 50, 150),   // Dark Orchid
        Color.FromRgb(155, 65, 155),   // Dark Magenta
        Color.FromRgb(160, 15, 100),   // Deep Pink
        Color.FromRgb(75, 0, 130),     // Indigo
        Color.FromRgb(120, 40, 140),   // Purple
        Color.FromRgb(145, 30, 180),   // Violet
        
        // Reds & Pinks (8 colors)
        Color.FromRgb(180, 15, 45),    // Deep Crimson
        Color.FromRgb(200, 50, 35),    // Dark Red
        Color.FromRgb(200, 50, 120),   // Dark Rose
        Color.FromRgb(190, 70, 70),    // Brick Red
        Color.FromRgb(165, 50, 50),    // Dark Coral
        Color.FromRgb(170, 0, 60),     // Maroon
        Color.FromRgb(220, 40, 85),    // Ruby Red
        Color.FromRgb(185, 25, 100),   // Deep Rose
        
        // Oranges & Yellows (8 colors)
        Color.FromRgb(210, 105, 0),    // Deep Orange
        Color.FromRgb(230, 120, 0),    // Vivid Orange
        Color.FromRgb(200, 160, 0),    // Dark Gold
        Color.FromRgb(165, 125, 20),   // Dark Goldenrod
        Color.FromRgb(180, 160, 50),   // Dark Khaki
        Color.FromRgb(190, 90, 0),     // Burnt Orange
        Color.FromRgb(170, 140, 0),    // Mustard
        Color.FromRgb(200, 140, 30),   // Amber
        
        // Browns & Earth Tones (10 colors)
        Color.FromRgb(160, 75, 20),    // Dark Chocolate
        Color.FromRgb(155, 95, 40),    // Burnt Sienna
        Color.FromRgb(140, 90, 90),    // Dark Rose Brown
        Color.FromRgb(120, 60, 30),    // Dark Sienna
        Color.FromRgb(100, 50, 15),    // Deep Brown
        Color.FromRgb(130, 70, 25),    // Saddle Brown
        Color.FromRgb(145, 85, 50),    // Copper
        Color.FromRgb(115, 80, 65),    // Coffee Brown
        Color.FromRgb(140, 100, 60),   // Bronze
        Color.FromRgb(105, 65, 40),    // Dark Tan
    };

    // Cache to ensure same category always gets same color
    private static readonly Dictionary<string, int> CategoryColorCache = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string category)
        {
            // Check cache first
            if (!CategoryColorCache.TryGetValue(category, out int colorIndex))
            {
                // Generate deterministic hash from category name using SHA256
                colorIndex = GetStableColorIndex(category);
                CategoryColorCache[category] = colorIndex;
            }
            
            return new SolidColorBrush(PredefinedColors[colorIndex]);
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static int GetStableColorIndex(string str)
    {
        // Use SHA256 for better distribution and fewer collisions
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
        
        // Use first 4 bytes to create an integer
        int hash = BitConverter.ToInt32(hashBytes, 0);
        
        // Map to color index with better distribution
        return Math.Abs(hash % PredefinedColors.Length);
    }
}
