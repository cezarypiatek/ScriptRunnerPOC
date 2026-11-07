using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.ViewModels;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class VaultBindingResolverTests
{
    [Fact]
    public void ResolveReturnsValueFromCurrentVaultSnapshot()
    {
        var vaultEntries = new List<VaultEntry>
        {
            new() { Name = "ApiToken", Secret = "updated-secret" }
        };

        var result = VaultBindingResolver.Resolve("ApiToken", vaultEntries);

        Assert.Equal(VaultEntryResolutionStatus.Resolved, result.Status);
        Assert.NotNull(result.Entry);
        Assert.Equal("updated-secret", result.Entry.Secret);
    }

    [Fact]
    public void ResolveReturnsNullWhenBoundEntryNoLongerExists()
    {
        var vaultEntries = new List<VaultEntry>
        {
            new() { Name = "DifferentToken", Secret = "secret" }
        };

        var result = VaultBindingResolver.Resolve("ApiToken", vaultEntries);

        Assert.Equal(VaultEntryResolutionStatus.Missing, result.Status);
        Assert.Null(result.Entry);
    }

    [Fact]
    public void ResolveUsesCaseSensitiveVaultKeys()
    {
        var vaultEntries = new List<VaultEntry>
        {
            new() { Name = "apitoken", Secret = "secret" }
        };

        var result = VaultBindingResolver.Resolve("ApiToken", vaultEntries);

        Assert.Equal(VaultEntryResolutionStatus.Missing, result.Status);
        Assert.Null(result.Entry);
    }

    [Fact]
    public void ResolveRejectsDuplicateVaultKeys()
    {
        var vaultEntries = new List<VaultEntry>
        {
            new() { Name = "ApiToken", Secret = "first" },
            new() { Name = "ApiToken", Secret = "second" }
        };

        var result = VaultBindingResolver.Resolve("ApiToken", vaultEntries);

        Assert.Equal(VaultEntryResolutionStatus.Duplicate, result.Status);
        Assert.Null(result.Entry);
    }
}
