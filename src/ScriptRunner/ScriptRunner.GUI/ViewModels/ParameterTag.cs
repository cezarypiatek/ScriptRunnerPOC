using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace ScriptRunner.GUI.ViewModels;

/// <summary>
/// Represents a single parameter as a tag for display
/// </summary>
public class ParameterTag
{
    public string Name { get; }
    public string Value { get; }
    public IBrush BackgroundBrush { get; }
    public bool IsMasked { get; }

    private static readonly List<Color> TagColors = new()
    {
        Color.FromRgb(52, 152, 219),   // Blue
        Color.FromRgb(155, 89, 182),   // Purple
        Color.FromRgb(46, 204, 113),   // Green
        Color.FromRgb(230, 126, 34),   // Orange
        Color.FromRgb(231, 76, 60),    // Red
        Color.FromRgb(26, 188, 156),   // Teal
        Color.FromRgb(241, 196, 15),   // Yellow
        Color.FromRgb(149, 165, 166),  // Gray
        Color.FromRgb(192, 57, 43),    // Dark Red
        Color.FromRgb(39, 174, 96),    // Dark Green
        Color.FromRgb(142, 68, 173),   // Dark Purple
        Color.FromRgb(41, 128, 185),   // Dark Blue
    };

    public ParameterTag(string name, string value, bool isMasked = false)
    {
        Name = name;
        Value = isMasked ? "*****" : value;
        IsMasked = isMasked;
        
        // Generate deterministic color based on parameter name
        var hash = GetStableHashCode(name);
        var colorIndex = Math.Abs(hash) % TagColors.Count;
        var color = TagColors[colorIndex];
        
        // Use semi-transparent color for background
        BackgroundBrush = new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B));
    }

    private static int GetStableHashCode(string str)
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

