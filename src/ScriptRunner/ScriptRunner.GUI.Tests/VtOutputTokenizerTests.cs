using ScriptRunner.GUI.Infrastructure;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class VtOutputTokenizerTests
{
    private const string Escape = "\u001b";

    [Fact]
    public void Tokenize_reads_osc8_link_with_st_terminator()
    {
        var input = $"before {Escape}]8;;https://example.com/a(b){Escape}\\click here{Escape}]8;;{Escape}\\ after";

        var tokens = VtOutputTokenizer.Tokenize(input);

        Assert.Collection(tokens,
            token => AssertText(token, "before "),
            token => AssertLinkStart(token, "https://example.com/a(b)"),
            token => AssertText(token, "click here"),
            token => Assert.Equal(VtOutputTokenKind.HyperlinkEnd, token.Kind),
            token => AssertText(token, " after"));
    }

    [Fact]
    public void Tokenize_reads_osc8_parameters_and_bel_terminator()
    {
        var input = $"{Escape}]8;id=result-1;https://example.com/a;b\u0007label{Escape}]8;;\u0007";

        var tokens = VtOutputTokenizer.Tokenize(input);

        Assert.Collection(tokens,
            token => AssertLinkStart(token, "https://example.com/a;b"),
            token => AssertText(token, "label"),
            token => Assert.Equal(VtOutputTokenKind.HyperlinkEnd, token.Kind));
    }

    [Theory]
    [InlineData("'")]
    [InlineData("\"")]
    public void Tokenize_removes_optional_quotes_around_target(string quote)
    {
        var input = $"{Escape}]8;;{quote}https://example.com/value{quote}{Escape}\\label{Escape}]8;;{Escape}\\";

        var tokens = VtOutputTokenizer.Tokenize(input);

        AssertLinkStart(tokens[0], "https://example.com/value");
    }

    [Fact]
    public void Tokenize_preserves_csi_sequences_for_existing_style_parser()
    {
        var input = $"plain{Escape}[31mred{Escape}[0m";

        var tokens = VtOutputTokenizer.Tokenize(input);

        Assert.Collection(tokens,
            token => AssertText(token, "plain"),
            token => Assert.Equal(VtOutputTokenKind.Csi, token.Kind),
            token => AssertText(token, "red"),
            token => Assert.Equal(VtOutputTokenKind.Csi, token.Kind));
    }

    [Fact]
    public void StripControlSequences_keeps_only_visible_label()
    {
        var input = $"{Escape}]8;;https://example.com{Escape}\\label{Escape}]8;;{Escape}\\";

        var result = VtOutputTokenizer.StripControlSequences(input);

        Assert.Equal("label", result);
    }

    private static void AssertText(VtOutputToken token, string expected)
    {
        Assert.Equal(VtOutputTokenKind.Text, token.Kind);
        Assert.Equal(expected, token.Text);
    }

    private static void AssertLinkStart(VtOutputToken token, string expectedTarget)
    {
        Assert.Equal(VtOutputTokenKind.HyperlinkStart, token.Kind);
        Assert.Equal(expectedTarget, token.Target);
    }
}
