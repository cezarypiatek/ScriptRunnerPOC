using ScriptRunner.GUI.ViewModels;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class RunningJobActionContextTests
{
    [Fact]
    public void CopiesInputCollectionsAndKeepsVaultBindingWithoutResolvedSecret()
    {
        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["text"] = "hello",
            ["token"] = "!!vault:ApiToken"
        };
        var passwords = new Dictionary<string, RunningJobPasswordInput>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = new(null, "ApiToken", false)
        };

        var context = new RunningJobActionContext("source", "action", inputs, passwords);
        inputs["text"] = "changed";
        passwords["token"] = new("resolved-secret", null, false);

        Assert.Equal("hello", context.InputValues["text"]);
        Assert.Equal("!!vault:ApiToken", context.InputValues["token"]);
        Assert.Equal("ApiToken", context.PasswordInputs["token"].VaultKey);
        Assert.Null(context.PasswordInputs["token"].ManualValue);
    }

    [Fact]
    public void KeepsManualPasswordThatLooksLikeAVaultReferenceExplicitlyManual()
    {
        var context = new RunningJobActionContext(
            "source",
            "action",
            new Dictionary<string, string> { ["token"] = "!!vault:not-a-binding" },
            new Dictionary<string, RunningJobPasswordInput>
            {
                ["token"] = new("!!vault:not-a-binding", null, false)
            });

        Assert.Null(context.PasswordInputs["token"].VaultKey);
        Assert.Equal("!!vault:not-a-binding", context.PasswordInputs["token"].ManualValue);
    }
}
