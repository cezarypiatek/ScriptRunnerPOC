using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptRunner.GUI.Infrastructure;

public enum OutputLinkKind
{
    Url,
    FilePath
}

public readonly record struct OutputLinkMatch(
    int Start,
    int Length,
    string Target,
    OutputLinkKind Kind);

/// <summary>
/// Finds clickable URLs and local paths in a single line of console output.
/// Detection is deliberately separate from rendering so the displayed and
/// clickable ranges cannot disagree.
/// </summary>
public static class OutputLinkDetector
{
    private const int MaximumPathCandidateLength = 2_048;
    private static readonly string[] SupportedSchemes = ["https://", "http://", "file://"];

    public static IReadOnlyList<OutputLinkMatch> Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<OutputLinkMatch>();
        }

        var candidates = new List<OutputLinkMatch>();
        FindUrls(text, candidates);
        FindPaths(text, candidates);

        // URL candidates take precedence over path-looking portions of URLs.
        var ordered = candidates
            .OrderBy(match => match.Start)
            .ThenBy(match => match.Kind == OutputLinkKind.Url ? 0 : 1)
            .ThenByDescending(match => match.Length);

        var result = new List<OutputLinkMatch>();
        var occupiedUntil = 0;
        foreach (var match in ordered)
        {
            if (match.Start < occupiedUntil)
            {
                continue;
            }

            result.Add(match);
            occupiedUntil = match.Start + match.Length;
        }

        return result;
    }

    private static void FindUrls(string text, ICollection<OutputLinkMatch> matches)
    {
        var searchFrom = 0;
        while (searchFrom < text.Length)
        {
            var (start, scheme) = FindNextScheme(text, searchFrom);
            if (start < 0 || scheme is null)
            {
                return;
            }

            var end = start + scheme.Length;
            while (end < text.Length && !IsUrlHardDelimiter(text[end]))
            {
                end++;
            }

            end = TrimUrlEnd(text, start, end);
            if (end > start && Uri.TryCreate(text[start..end], UriKind.Absolute, out var uri))
            {
                if (uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host))
                {
                    matches.Add(new OutputLinkMatch(start, end - start, text[start..end], OutputLinkKind.Url));
                }
                else if (uri.IsFile && TryGetExistingPath(uri.LocalPath, out var localPath))
                {
                    matches.Add(new OutputLinkMatch(start, end - start, localPath, OutputLinkKind.FilePath));
                }
            }

            searchFrom = Math.Max(end, start + scheme.Length);
        }
    }

    private static (int Start, string? Scheme) FindNextScheme(string text, int searchFrom)
    {
        var earliest = -1;
        string? selectedScheme = null;

        foreach (var scheme in SupportedSchemes)
        {
            var candidate = text.IndexOf(scheme, searchFrom, StringComparison.OrdinalIgnoreCase);
            while (candidate >= 0 && candidate > 0 && IsSchemeCharacter(text[candidate - 1]))
            {
                candidate = text.IndexOf(scheme, candidate + scheme.Length, StringComparison.OrdinalIgnoreCase);
            }

            if (candidate >= 0 && (earliest < 0 || candidate < earliest))
            {
                earliest = candidate;
                selectedScheme = scheme;
            }
        }

        return (earliest, selectedScheme);
    }

    private static bool IsSchemeCharacter(char value) =>
        char.IsLetterOrDigit(value) || value is '+' or '-' or '.';

    private static bool IsUrlHardDelimiter(char value) =>
        char.IsWhiteSpace(value) || char.IsControl(value) || value is '<' or '>' or '"' or '`';

    private static int TrimUrlEnd(string text, int start, int end)
    {
        var changed = true;
        while (end > start && changed)
        {
            changed = false;

            if (text[end - 1] is '.' or ',' or ';' or ':' or '!')
            {
                end--;
                changed = true;
                continue;
            }

            // A matching single quote around a URL is prose punctuation.
            // Apostrophes elsewhere remain part of the URL because RFC 3986
            // permits them.
            if (start > 0 && text[start - 1] == '\'' && text[end - 1] == '\'')
            {
                end--;
                changed = true;
                continue;
            }

            if (IsUnmatchedClosingDelimiter(text, start, end, '(', ')') ||
                IsUnmatchedClosingDelimiter(text, start, end, '[', ']') ||
                IsUnmatchedClosingDelimiter(text, start, end, '{', '}'))
            {
                end--;
                changed = true;
            }
        }

        return end;
    }

    private static bool IsUnmatchedClosingDelimiter(
        string text,
        int start,
        int end,
        char opening,
        char closing)
    {
        if (end <= start || text[end - 1] != closing)
        {
            return false;
        }

        var balance = 0;
        for (var index = start; index < end; index++)
        {
            if (text[index] == opening)
            {
                balance++;
            }
            else if (text[index] == closing)
            {
                balance--;
            }
        }

        return balance < 0;
    }

    private static void FindPaths(string text, ICollection<OutputLinkMatch> matches)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (!IsPathStart(text, index))
            {
                continue;
            }

            var candidateEnd = index;
            var maximumEnd = Math.Min(text.Length, index + MaximumPathCandidateLength);
            while (candidateEnd < maximumEnd && !IsPathHardDelimiter(text[candidateEnd]))
            {
                candidateEnd++;
            }

            if (TryFindExistingPathPrefix(text, index, candidateEnd, out var match))
            {
                matches.Add(match);
                index = match.Start + match.Length - 1;
            }
        }
    }

    private static bool IsPathStart(string text, int index)
    {
        var hasBoundary = index == 0 || IsPathBoundary(text[index - 1]);
        if (!hasBoundary)
        {
            return false;
        }

        if (index + 2 < text.Length && char.IsAsciiLetter(text[index]) && text[index + 1] == ':' &&
            text[index + 2] is '\\' or '/')
        {
            return true;
        }

        if (index + 1 < text.Length && text[index] == '\\' && text[index + 1] == '\\')
        {
            return true;
        }

        if (text[index] == '/')
        {
            return true;
        }

        return index + 1 < text.Length && text[index] == '~' && text[index + 1] == '/';
    }

    private static bool IsPathBoundary(char value) =>
        char.IsWhiteSpace(value) || value is '"' or '\'' or '`' or '(' or '[' or '{' or '=';

    private static bool IsPathHardDelimiter(char value) =>
        char.IsControl(value) || value is '"' or '\'' or '`' or '<' or '>' or '|';

    private static bool TryFindExistingPathPrefix(
        string text,
        int start,
        int candidateEnd,
        out OutputLinkMatch match)
    {
        for (var end = candidateEnd; end > start; end--)
        {
            if (end < candidateEnd && !IsPossiblePathEndBoundary(text[end]))
            {
                continue;
            }

            var untrimmedEnd = end;
            while (end > start && char.IsWhiteSpace(text[end - 1]))
            {
                end--;
            }

            if (end <= start)
            {
                end = untrimmedEnd;
                continue;
            }

            var candidate = text[start..end];
            if (TryGetExistingPath(candidate, out var path))
            {
                match = new OutputLinkMatch(start, end - start, path, OutputLinkKind.FilePath);
                return true;
            }

            end = untrimmedEnd;
        }

        match = default;
        return false;
    }

    private static bool IsPossiblePathEndBoundary(char value) =>
        char.IsWhiteSpace(value) || value is '(' or ')' or '[' or ']' or '{' or '}' or ':' or ',' or ';' or '.' or '!';

    private static bool TryGetExistingPath(string candidate, out string path)
    {
        path = candidate;
        try
        {
            if (candidate.StartsWith("~/", StringComparison.Ordinal))
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    candidate[2..].Replace('/', Path.DirectorySeparatorChar));
            }

            return File.Exists(path) || Directory.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
