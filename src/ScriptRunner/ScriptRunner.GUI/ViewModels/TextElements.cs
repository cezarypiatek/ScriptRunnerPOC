using Avalonia.Media;

namespace ScriptRunner.GUI.ViewModels;

public abstract record OutputElement;

public record LineEnding : OutputElement
{
    public static readonly LineEnding Instance = new LineEnding();
}
public record TextSpan(string Text, IBrush Foreground, IBrush BackGround,  bool IsBold = false, bool IsItalic = false, bool IsUnderline = false, bool IsStrikethrough = false):OutputElement;
public record Link(string Text, string? Url = null):OutputElement;