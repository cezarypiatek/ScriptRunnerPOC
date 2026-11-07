using ScriptRunner.GUI.Settings;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class SettingsScopeResolverTests
{
    private sealed record Setting(string? Source, string Value);

    [Fact]
    public void FindBest_PrefersExactSourceOverLegacyFallback()
    {
        var settings = new[]
        {
            new Setting(null, "legacy"),
            new Setting("source-a", "specific")
        };

        var result = SettingsScopeResolver.FindBest(settings, "source-a", x => x.Source);

        Assert.Equal("specific", result!.Value);
    }

    [Fact]
    public void FindBest_UsesLegacyButNeverAnotherSource()
    {
        var settings = new[]
        {
            new Setting("source-a", "wrong-source"),
            new Setting(null, "legacy")
        };

        var result = SettingsScopeResolver.FindBest(settings, "source-b", x => x.Source);

        Assert.Equal("legacy", result!.Value);
    }

    [Fact]
    public void FindBest_UsesNewestDuplicateWithinTheWinningScope()
    {
        var settings = new[]
        {
            new Setting("source-a", "old"),
            new Setting(null, "legacy"),
            new Setting("source-a", "new")
        };

        var result = SettingsScopeResolver.FindBest(settings, "source-a", x => x.Source);

        Assert.Equal("new", result!.Value);
    }

    [Fact]
    public void OrderForOverlay_AppliesLegacyBeforeExactAndExcludesOtherSources()
    {
        var settings = new[]
        {
            new Setting("source-a", "specific"),
            new Setting("source-b", "wrong-source"),
            new Setting(null, "legacy")
        };

        var result = SettingsScopeResolver.OrderForOverlay(settings, "source-a", x => x.Source)
            .Select(x => x.Value)
            .ToArray();

        Assert.Equal(new[] { "legacy", "specific" }, result);
    }
}
