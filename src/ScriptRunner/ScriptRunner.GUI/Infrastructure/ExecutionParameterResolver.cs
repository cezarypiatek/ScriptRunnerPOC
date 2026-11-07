using System;
using System.Collections.Generic;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Infrastructure;

public sealed record ExecutionParameterInput(
    string Name,
    string? CurrentValue,
    bool IsPassword,
    string? VaultKey,
    bool HasInvalidVaultReference = false);

public sealed record VaultBindingIssue(
    string ParameterName,
    string VaultKey,
    VaultEntryResolutionStatus Status);

public sealed record ResolvedVaultParameter(
    string ParameterName,
    string VaultKey,
    VaultEntry Entry);

public sealed class ExecutionParameterResolution
{
    public Dictionary<string, string?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<VaultBindingIssue> VaultIssues { get; } = new();
    public List<ResolvedVaultParameter> ResolvedVaultParameters { get; } = new();
}

public static class ExecutionParameterResolver
{
    public static ExecutionParameterResolution Resolve(
        IEnumerable<ExecutionParameterInput> parameters,
        IReadOnlyList<VaultEntry> vaultEntries)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(vaultEntries);

        var result = new ExecutionParameterResolution();
        foreach (var parameter in parameters)
        {
            if (!parameter.IsPassword)
            {
                result.Values[parameter.Name] = parameter.CurrentValue?.Trim();
                continue;
            }

            if (parameter.HasInvalidVaultReference)
            {
                result.Values[parameter.Name] = null;
                result.VaultIssues.Add(new VaultBindingIssue(
                    parameter.Name,
                    parameter.CurrentValue ?? VaultReference.Prefix,
                    VaultEntryResolutionStatus.InvalidReference));
                continue;
            }

            var vaultKey = parameter.VaultKey;
            if (string.IsNullOrWhiteSpace(vaultKey))
            {
                // Passwords are opaque values. Reference parsing happens when serialized input is
                // loaded into the password control; execution trusts the control's explicit state.
                result.Values[parameter.Name] = parameter.CurrentValue;
                continue;
            }

            var vaultResolution = VaultBindingResolver.Resolve(vaultKey!, vaultEntries);
            if (vaultResolution is { Status: VaultEntryResolutionStatus.Resolved, Entry: { } entry })
            {
                result.Values[parameter.Name] = entry.Secret;
                result.ResolvedVaultParameters.Add(new ResolvedVaultParameter(
                    parameter.Name,
                    vaultKey!,
                    entry));
                continue;
            }

            result.Values[parameter.Name] = null;
            result.VaultIssues.Add(new VaultBindingIssue(
                parameter.Name,
                vaultKey!,
                vaultResolution.Status));
        }

        return result;
    }
}
