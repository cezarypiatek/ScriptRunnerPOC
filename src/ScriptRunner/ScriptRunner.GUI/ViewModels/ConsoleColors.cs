using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace ScriptRunner.GUI.ViewModels;

public static class ConsoleColors
{
    private static readonly ImmutableSolidColorBrush[] Normal =
    [
        new(Color.FromRgb(0, 0, 0)),
        new(Color.FromRgb(170, 0, 0)),
        new(Color.FromRgb(0, 170, 0)),
        new(Color.FromRgb(170, 85, 0)),
        new(Color.FromRgb(0, 0, 170)),
        new(Color.FromRgb(170, 0, 170)),
        new(Color.FromRgb(0, 170, 170)),
        new(Color.FromRgb(170, 170, 170))
    ];

    private static readonly ImmutableSolidColorBrush[] Bright =
    [
        new(Color.FromRgb(85, 85, 85)),
        new(Color.FromRgb(255, 85, 85)),
        new(Color.FromRgb(85, 255, 85)),
        new(Color.FromRgb(255, 255, 85)),
        new(Color.FromRgb(85, 85, 255)),
        new(Color.FromRgb(255, 85, 255)),
        new(Color.FromRgb(85, 255, 255)),
        new(Color.FromRgb(255, 255, 255))
    ];

    public static ImmutableSolidColorBrush Get(int index, bool bright) =>
        (bright ? Bright : Normal)[index];
}
