using System;
using System.Collections.Generic;
using System.Linq;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Infrastructure;

public static class VaultBindingResolver
{
    public static VaultEntryResolution Resolve(string vaultKey, IReadOnlyList<VaultEntry> vaultEntries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultKey);
        ArgumentNullException.ThrowIfNull(vaultEntries);

        var matches = vaultEntries
            .Where(entry => string.Equals(entry.Name, vaultKey, StringComparison.Ordinal))
            .Take(2)
            .ToList();

        return matches.Count switch
        {
            0 => new VaultEntryResolution(VaultEntryResolutionStatus.Missing, null),
            1 => new VaultEntryResolution(VaultEntryResolutionStatus.Resolved, matches[0]),
            _ => new VaultEntryResolution(VaultEntryResolutionStatus.Duplicate, null)
        };
    }
}

public enum VaultEntryResolutionStatus
{
    Resolved,
    Missing,
    Duplicate,
    InvalidReference
}

public sealed record VaultEntryResolution(VaultEntryResolutionStatus Status, VaultEntry? Entry);
