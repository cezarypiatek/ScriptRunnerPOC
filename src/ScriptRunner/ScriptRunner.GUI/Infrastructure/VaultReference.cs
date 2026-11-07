using System;

namespace ScriptRunner.GUI.Infrastructure;

public static class VaultReference
{
    public const string Prefix = "!!vault:";

    public static bool TryParse(string? value, out string vaultKey)
    {
        var result = Parse(value);
        if (result.Status == VaultReferenceStatus.Valid)
        {
            vaultKey = result.VaultKey!;
            return true;
        }

        vaultKey = string.Empty;
        return false;
    }

    public static VaultReferenceParseResult Parse(string? value)
    {
        if (value?.StartsWith(Prefix, StringComparison.Ordinal) != true)
        {
            return new VaultReferenceParseResult(VaultReferenceStatus.NotReference, null);
        }

        var vaultKey = value[Prefix.Length..];
        return string.IsNullOrWhiteSpace(vaultKey)
            ? new VaultReferenceParseResult(VaultReferenceStatus.Invalid, null)
            : new VaultReferenceParseResult(VaultReferenceStatus.Valid, vaultKey);
    }

    public static string Format(string vaultKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultKey);
        return $"{Prefix}{vaultKey}";
    }
}

public enum VaultReferenceStatus
{
    NotReference,
    Valid,
    Invalid
}

public sealed record VaultReferenceParseResult(VaultReferenceStatus Status, string? VaultKey);
