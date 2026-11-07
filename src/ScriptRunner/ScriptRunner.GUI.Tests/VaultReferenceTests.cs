using ScriptRunner.GUI.Infrastructure;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class VaultReferenceTests
{
    [Fact]
    public void ReferenceRoundTrips()
    {
        var reference = VaultReference.Format("ApiToken");

        var parsed = VaultReference.TryParse(reference, out var vaultKey);

        Assert.True(parsed);
        Assert.Equal("ApiToken", vaultKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plain-secret")]
    [InlineData("!!vault:")]
    public void InvalidReferenceIsNotParsed(string? value)
    {
        Assert.False(VaultReference.TryParse(value, out _));
    }

    [Theory]
    [InlineData("!!vault:")]
    [InlineData("!!vault:   ")]
    public void EmptyVaultKeyIsAnInvalidReference(string value)
    {
        var result = VaultReference.Parse(value);

        Assert.Equal(VaultReferenceStatus.Invalid, result.Status);
    }
}
