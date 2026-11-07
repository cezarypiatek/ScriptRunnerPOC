using ScriptRunner.GUI.Infrastructure;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class HiddenVaultKeyTests
{
    [Fact]
    public void Create_IsStableAndHidden()
    {
        var first = HiddenVaultKey.Create("source", "action", "execution", "password");
        var second = HiddenVaultKey.Create("source", "action", "execution", "password");

        Assert.Equal(first, second);
        Assert.StartsWith("!ScriptRunner:", first);
    }

    [Fact]
    public void Create_DoesNotCollideForAmbiguousNames()
    {
        var first = HiddenVaultKey.Create("a_b", "c", "preset:d", "password");
        var second = HiddenVaultKey.Create("a", "b_c", "preset:d", "password");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_SeparatesScopesAndParameters()
    {
        var execution = HiddenVaultKey.Create("source", "action", "execution", "password");
        var defaults = HiddenVaultKey.Create("source", "action", "default", "password");
        var otherParameter = HiddenVaultKey.Create("source", "action", "execution", "token");

        Assert.NotEqual(execution, defaults);
        Assert.NotEqual(execution, otherParameter);
    }
}
