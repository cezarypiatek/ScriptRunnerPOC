using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.ViewModels;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class ExecutionParameterResolverTests
{
    [Fact]
    public void ResolvesBoundPasswordFromCurrentVaultSnapshot()
    {
        var inputs = new[]
        {
            new ExecutionParameterInput("token", "cached-secret", true, "ApiToken")
        };
        var vault = new[]
        {
            new VaultEntry { Name = "ApiToken", Secret = "updated-secret" }
        };

        var result = ExecutionParameterResolver.Resolve(inputs, vault);

        Assert.Equal("updated-secret", result.Values["token"]);
        Assert.Empty(result.VaultIssues);
    }

    [Fact]
    public void PreservesPasswordWhitespaceButTrimsOrdinaryParameters()
    {
        var inputs = new[]
        {
            new ExecutionParameterInput("password", "  exact secret  ", true, null),
            new ExecutionParameterInput("name", "  ordinary value  ", false, null)
        };

        var result = ExecutionParameterResolver.Resolve(inputs, Array.Empty<VaultEntry>());

        Assert.Equal("  exact secret  ", result.Values["password"]);
        Assert.Equal("ordinary value", result.Values["name"]);
    }

    [Fact]
    public void MissingBindingNeverFallsBackToCachedPassword()
    {
        var inputs = new[]
        {
            new ExecutionParameterInput("token", "cached-secret", true, "MissingToken")
        };

        var result = ExecutionParameterResolver.Resolve(inputs, Array.Empty<VaultEntry>());

        Assert.Null(result.Values["token"]);
        var issue = Assert.Single(result.VaultIssues);
        Assert.Equal(VaultEntryResolutionStatus.Missing, issue.Status);
    }

    [Fact]
    public void DuplicateBindingNeverSelectsAnArbitrarySecret()
    {
        var inputs = new[]
        {
            new ExecutionParameterInput("token", "cached-secret", true, "ApiToken")
        };
        var vault = new[]
        {
            new VaultEntry { Name = "ApiToken", Secret = "first" },
            new VaultEntry { Name = "ApiToken", Secret = "second" }
        };

        var result = ExecutionParameterResolver.Resolve(inputs, vault);

        Assert.Null(result.Values["token"]);
        var issue = Assert.Single(result.VaultIssues);
        Assert.Equal(VaultEntryResolutionStatus.Duplicate, issue.Status);
    }

    [Fact]
    public void MalformedVaultReferenceIsRejectedInsteadOfUsedAsPassword()
    {
        var inputs = new[]
        {
            new ExecutionParameterInput("token", "!!vault:", true, null, HasInvalidVaultReference: true)
        };

        var result = ExecutionParameterResolver.Resolve(inputs, Array.Empty<VaultEntry>());

        Assert.Null(result.Values["token"]);
        var issue = Assert.Single(result.VaultIssues);
        Assert.Equal(VaultEntryResolutionStatus.InvalidReference, issue.Status);
    }

    [Fact]
    public void ManualPasswordThatLooksLikeVaultReferenceRemainsOpaque()
    {
        var inputs = new[]
        {
            new ExecutionParameterInput("token", "!!vault:ApiToken", true, null)
        };
        var result = ExecutionParameterResolver.Resolve(inputs, Array.Empty<VaultEntry>());

        Assert.Equal("!!vault:ApiToken", result.Values["token"]);
        Assert.Empty(result.VaultIssues);
    }
}
