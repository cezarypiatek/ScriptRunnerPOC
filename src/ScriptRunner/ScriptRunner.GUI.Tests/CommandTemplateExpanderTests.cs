using ScriptRunner.GUI.Infrastructure;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class CommandTemplateExpanderTests
{
    [Fact]
    public void ReplacementValuesAreNotExpandedAgain()
    {
        var values = new Dictionary<string, string?>
        {
            ["password"] = "secret-{environment}",
            ["environment"] = "production"
        };

        var result = CommandTemplateExpander.Expand("tool --password {password} --env {environment}", values);

        Assert.Equal("tool --password secret-{environment} --env production", result);
    }

    [Fact]
    public void UnknownPlaceholdersArePreserved()
    {
        var result = CommandTemplateExpander.Expand(
            "tool {known} {unknown}",
            new Dictionary<string, string?> { ["known"] = "value" });

        Assert.Equal("tool value {unknown}", result);
    }

    [Fact]
    public void NullValuesExpandToEmptyText()
    {
        var result = CommandTemplateExpander.Expand(
            "tool before{value}after",
            new Dictionary<string, string?> { ["value"] = null });

        Assert.Equal("tool beforeafter", result);
    }

    [Fact]
    public void ReferencedParameterNamesAreCollectedCaseInsensitivelyAcrossTemplates()
    {
        var result = CommandTemplateExpander.GetReferencedParameterNames(new string?[]
        {
            "tool --token {Token} --name {name}",
            "prefix-{token}",
            null
        });

        Assert.Equal(2, result.Count);
        Assert.Contains("token", result);
        Assert.Contains("name", result);
    }
}
