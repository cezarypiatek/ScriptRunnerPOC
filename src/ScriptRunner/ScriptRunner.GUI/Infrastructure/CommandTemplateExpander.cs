using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScriptRunner.GUI.Infrastructure;

public static partial class CommandTemplateExpander
{
    [GeneratedRegex(@"\{(?<name>[^{}]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();

    public static string Expand(
        string template,
        IReadOnlyDictionary<string, string?> parameterValues)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(parameterValues);

        // Regex.Replace evaluates matches from the original template, so placeholder-looking
        // text inside a replacement value is never interpreted a second time.
        return PlaceholderPattern().Replace(template, match =>
        {
            var parameterName = match.Groups["name"].Value;
            return parameterValues.TryGetValue(parameterName, out var value)
                ? value ?? string.Empty
                : match.Value;
        });
    }

    public static IReadOnlySet<string> GetReferencedParameterNames(IEnumerable<string?> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        return templates
            .Where(template => template is not null)
            .SelectMany(template => PlaceholderPattern().Matches(template!).Select(match =>
                match.Groups["name"].Value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
