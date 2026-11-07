using System;

namespace ScriptRunner.GUI.Infrastructure;

public static class VaultKeyPolicy
{
    // Keys beginning with ! have historically been reserved for ScriptRunner-managed secrets.
    public static bool IsInternal(string? vaultKey) =>
        vaultKey?.StartsWith("!", StringComparison.Ordinal) == true;

    public static bool IsUserSelectable(string? vaultKey) =>
        !string.IsNullOrWhiteSpace(vaultKey) && !IsInternal(vaultKey);
}
