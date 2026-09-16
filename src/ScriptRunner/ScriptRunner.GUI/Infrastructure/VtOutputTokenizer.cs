using System;
using System.Collections.Generic;
using System.Text;

namespace ScriptRunner.GUI.Infrastructure;

public enum VtOutputTokenKind
{
    Text,
    Csi,
    HyperlinkStart,
    HyperlinkEnd,
    Control
}

public readonly record struct VtOutputToken(
    VtOutputTokenKind Kind,
    string Text,
    string? Target = null);

/// <summary>
/// Tokenizes the VT sequences used by job output. In addition to CSI styling,
/// this understands OSC 8 hyperlinks terminated by ST (ESC \ or U+009C) or BEL.
/// </summary>
public static class VtOutputTokenizer
{
    private const char Escape = '\u001b';
    private const char Bell = '\u0007';
    private const char StringTerminator = '\u009c';

    public static IReadOnlyList<VtOutputToken> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<VtOutputToken>();
        }

        var result = new List<VtOutputToken>();
        var textStart = 0;
        var index = 0;

        while (index < text.Length)
        {
            if (text[index] != Escape)
            {
                index++;
                continue;
            }

            AddText(result, text, textStart, index);

            if (index + 1 >= text.Length)
            {
                AddText(result, text, index, text.Length);
                return result;
            }

            if (text[index + 1] == '[')
            {
                var end = FindCsiEnd(text, index + 2);
                if (end < 0)
                {
                    AddText(result, text, index, text.Length);
                    return result;
                }

                result.Add(new VtOutputToken(
                    VtOutputTokenKind.Csi,
                    text[index..(end + 1)]));
                index = end + 1;
                textStart = index;
                continue;
            }

            if (text[index + 1] == ']')
            {
                var (payloadEnd, sequenceEnd) = FindOscEnd(text, index + 2);
                if (payloadEnd < 0)
                {
                    AddText(result, text, index, text.Length);
                    return result;
                }

                var payload = text[(index + 2)..payloadEnd];
                AddOscToken(result, payload);
                index = sequenceEnd;
                textStart = index;
                continue;
            }

            if (text[index + 1] is 'P' or '_' or '^')
            {
                var sequenceEnd = FindStringControlEnd(text, index + 2);
                if (sequenceEnd < 0)
                {
                    AddText(result, text, index, text.Length);
                    return result;
                }

                result.Add(new VtOutputToken(
                    VtOutputTokenKind.Control,
                    text[index..sequenceEnd]));
                index = sequenceEnd;
                textStart = index;
                continue;
            }

            // Unknown two-byte escape. Keep it out of visible output while
            // allowing parsing to continue with the following text.
            result.Add(new VtOutputToken(VtOutputTokenKind.Control, text.Substring(index, 2)));
            index += 2;
            textStart = index;
        }

        AddText(result, text, textStart, text.Length);
        return result;
    }

    public static string StripControlSequences(string text)
    {
        var result = new StringBuilder(text.Length);
        foreach (var token in Tokenize(text))
        {
            if (token.Kind == VtOutputTokenKind.Text)
            {
                result.Append(token.Text);
            }
        }

        return result.ToString();
    }

    private static void AddText(ICollection<VtOutputToken> result, string source, int start, int end)
    {
        if (end > start)
        {
            result.Add(new VtOutputToken(VtOutputTokenKind.Text, source[start..end]));
        }
    }

    private static int FindCsiEnd(string text, int start)
    {
        for (var index = start; index < text.Length; index++)
        {
            // ECMA-48 final bytes occupy the range 0x40 through 0x7E.
            if (text[index] is >= '@' and <= '~')
            {
                return index;
            }
        }

        return -1;
    }

    private static (int PayloadEnd, int SequenceEnd) FindOscEnd(string text, int start)
    {
        for (var index = start; index < text.Length; index++)
        {
            if (text[index] is Bell or StringTerminator)
            {
                return (index, index + 1);
            }

            if (text[index] == Escape && index + 1 < text.Length && text[index + 1] == '\\')
            {
                return (index, index + 2);
            }
        }

        return (-1, -1);
    }

    private static int FindStringControlEnd(string text, int start)
    {
        for (var index = start; index < text.Length; index++)
        {
            if (text[index] == StringTerminator)
            {
                return index + 1;
            }

            if (text[index] == Escape && index + 1 < text.Length && text[index + 1] == '\\')
            {
                return index + 2;
            }
        }

        return -1;
    }

    private static void AddOscToken(ICollection<VtOutputToken> result, string payload)
    {
        // OSC 8 format: "8;params;URI". Params may be empty and URI may
        // contain additional semicolons, so only locate the params separator.
        if (!payload.StartsWith("8;", StringComparison.Ordinal))
        {
            result.Add(new VtOutputToken(VtOutputTokenKind.Control, payload));
            return;
        }

        var uriSeparator = payload.IndexOf(';', 2);
        if (uriSeparator < 0)
        {
            result.Add(new VtOutputToken(VtOutputTokenKind.Control, payload));
            return;
        }

        var target = UnquoteTarget(payload[(uriSeparator + 1)..].Trim());
        result.Add(string.IsNullOrEmpty(target)
            ? new VtOutputToken(VtOutputTokenKind.HyperlinkEnd, string.Empty)
            : new VtOutputToken(VtOutputTokenKind.HyperlinkStart, string.Empty, target));
    }

    private static string UnquoteTarget(string target)
    {
        if (target.Length >= 2 &&
            ((target[0] == '"' && target[^1] == '"') ||
             (target[0] == '\'' && target[^1] == '\'')))
        {
            return target[1..^1];
        }

        return target;
    }
}

/// <summary>
/// Preserves an incomplete VT sequence between process-output chunks.
/// </summary>
public sealed class VtOutputStreamTokenizer
{
    private string _pending = string.Empty;
    public bool HasPendingSequence => _pending.Length > 0;

    public IReadOnlyList<VtOutputToken> TokenizeChunk(string chunk)
    {
        var input = _pending.Length == 0 ? chunk : _pending + chunk;
        _pending = string.Empty;

        var incompleteStart = FindIncompleteSequenceStart(input);
        if (incompleteStart >= 0)
        {
            _pending = input[incompleteStart..];
            input = input[..incompleteStart];
        }

        return VtOutputTokenizer.Tokenize(input);
    }

    public IReadOnlyList<VtOutputToken> Flush()
    {
        if (_pending.Length == 0)
        {
            return Array.Empty<VtOutputToken>();
        }

        var pending = _pending;
        _pending = string.Empty;
        return [new VtOutputToken(VtOutputTokenKind.Text, pending)];
    }

    private static int FindIncompleteSequenceStart(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\u001b')
            {
                continue;
            }

            if (index + 1 >= text.Length)
            {
                return index;
            }

            switch (text[index + 1])
            {
                case '[':
                    var csiEnd = FindCsiEnd(text, index + 2);
                    if (csiEnd < 0)
                    {
                        return index;
                    }
                    index = csiEnd;
                    break;

                case ']':
                    var oscEnd = FindStringEnd(text, index + 2, allowBell: true);
                    if (oscEnd < 0)
                    {
                        return index;
                    }
                    index = oscEnd - 1;
                    break;

                case 'P':
                case '_':
                case '^':
                    var stringEnd = FindStringEnd(text, index + 2, allowBell: false);
                    if (stringEnd < 0)
                    {
                        return index;
                    }
                    index = stringEnd - 1;
                    break;

                default:
                    index++;
                    break;
            }
        }

        return -1;
    }

    private static int FindCsiEnd(string text, int start)
    {
        for (var index = start; index < text.Length; index++)
        {
            if (text[index] is >= '@' and <= '~')
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindStringEnd(string text, int start, bool allowBell)
    {
        for (var index = start; index < text.Length; index++)
        {
            if ((allowBell && text[index] == '\u0007') || text[index] == '\u009c')
            {
                return index + 1;
            }

            if (text[index] == '\u001b' && index + 1 < text.Length && text[index + 1] == '\\')
            {
                return index + 2;
            }
        }

        return -1;
    }
}
