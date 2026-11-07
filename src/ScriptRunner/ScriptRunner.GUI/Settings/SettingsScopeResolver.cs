using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptRunner.GUI.Settings;

/// <summary>
/// Resolves settings introduced before source scoping (SourceName is null) alongside current,
/// source-specific settings. An exact source always wins; legacy records remain a fallback.
/// </summary>
public static class SettingsScopeResolver
{
    public static T? FindBest<T>(
        IEnumerable<T> candidates,
        string? sourceName,
        Func<T, string?> sourceSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(sourceSelector);

        var applicable = candidates
            .Where(candidate => IsApplicable(sourceSelector(candidate), sourceName))
            .ToList();

        // Settings updates append their replacement record. If an older version or interrupted
        // write left duplicates behind, the newest exact record is the deterministic winner.
        return applicable.LastOrDefault(candidate => IsExact(sourceSelector(candidate), sourceName))
               ?? applicable.LastOrDefault(candidate => sourceSelector(candidate) is null);
    }

    public static IEnumerable<T> OrderForOverlay<T>(
        IEnumerable<T> candidates,
        string? sourceName,
        Func<T, string?> sourceSelector)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(sourceSelector);

        // Legacy records are applied first so source-specific records can replace them.
        return candidates
            .Where(candidate => IsApplicable(sourceSelector(candidate), sourceName))
            .OrderBy(candidate => IsExact(sourceSelector(candidate), sourceName));
    }

    private static bool IsApplicable(string? candidateSource, string? requestedSource) =>
        candidateSource is null || IsExact(candidateSource, requestedSource);

    private static bool IsExact(string? candidateSource, string? requestedSource) =>
        string.Equals(candidateSource, requestedSource, StringComparison.Ordinal);
}
