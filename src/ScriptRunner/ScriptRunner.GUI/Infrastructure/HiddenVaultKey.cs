using System;
using System.Security.Cryptography;
using System.Text;

namespace ScriptRunner.GUI.Infrastructure;

/// <summary>
/// Creates stable, non-colliding keys for passwords that ScriptRunner stores in the Vault.
/// Length prefixes make the identity unambiguous before it is hashed (for example, names that
/// contain underscores or colons cannot accidentally address the same entry).
/// </summary>
public static class HiddenVaultKey
{
    private const string Prefix = "!ScriptRunner:";

    public static string Create(
        string? sourceName,
        string actionName,
        string scope,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        var identity = string.Concat(
            Encode(sourceName ?? string.Empty),
            Encode(actionName),
            Encode(scope),
            Encode(parameterName));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Prefix + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Encode(string value) => $"{value.Length}:{value}";
}
