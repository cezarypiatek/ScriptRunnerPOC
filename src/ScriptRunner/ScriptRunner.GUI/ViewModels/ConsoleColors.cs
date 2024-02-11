using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace ScriptRunner.GUI.ViewModels;

public static class ConsoleColors
{
    public static readonly ImmutableSolidColorBrush BrightBlack = new(Color.FromRgb(85, 85, 85));
    public static readonly ImmutableSolidColorBrush BrightRed = new(Color.FromRgb(255, 85, 85));
    public static readonly ImmutableSolidColorBrush BrightGreen = new(Color.FromRgb(85, 255, 85));
    public static readonly ImmutableSolidColorBrush BrightYellow = new(Color.FromRgb(255, 255, 85));
    public static readonly ImmutableSolidColorBrush BrightBlue = new(Color.FromRgb(85, 85, 255));
    public static readonly ImmutableSolidColorBrush BrightMagenta = new(Color.FromRgb(255, 85, 255));
    public static readonly ImmutableSolidColorBrush BrightCyan = new(Color.FromRgb(85, 255, 255));
    public static readonly ImmutableSolidColorBrush BrightWhite = new(Color.FromRgb(255, 255, 255));
}