using ScriptRunner.GUI.Infrastructure;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class VaultKeyPolicyTests
{
    [Theory]
    [InlineData("PublicKey", true)]
    [InlineData("!ScriptRunner:managed", false)]
    [InlineData("!legacy-managed", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void UserSelectableKeysFollowReservedNamePolicy(string? key, bool expected)
    {
        Assert.Equal(expected, VaultKeyPolicy.IsUserSelectable(key));
    }
}
