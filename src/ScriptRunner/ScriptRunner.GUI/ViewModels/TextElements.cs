using Avalonia.Media;

namespace ScriptRunner.GUI.ViewModels;

public abstract record OutputElement;

public record LineEnding : OutputElement
{
    public static readonly LineEnding Instance = new LineEnding();
}

public sealed record OutputTextStyle(
    IBrush Foreground,
    IBrush Background,
    bool IsBold = false,
    bool IsItalic = false,
    bool IsUnderline = false,
    bool IsDoubleUnderline = false,
    bool IsStrikethrough = false,
    bool IsOverline = false,
    IBrush? UnderlineColor = null);

public record TextSpan(string Text, OutputTextStyle Style) : OutputElement;
public record Link(
    string Text,
    string? Url,
    OutputTextStyle Style,
    bool UseLinkAppearance = true) : OutputElement;
