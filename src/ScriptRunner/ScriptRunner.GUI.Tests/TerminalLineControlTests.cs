using ScriptRunner.GUI.ViewModels;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class TerminalLineControlTests
{
    [Theory]
    [InlineData("abcdef\rXY", "XYcdef")]
    [InlineData("abc\bZ", "abZ")]
    [InlineData("a\tZ", "a       Z")]
    public void Text_controls_update_the_current_terminal_line(string input, string expected)
    {
        var viewModel = new RunningJobViewModel();
        var cells = new List<RunningJobViewModel.TerminalCell>();
        var cursor = 0;

        RunningJobViewModel.WriteTerminalText(
            cells,
            ref cursor,
            input,
            viewModel.GetCurrentTextStyle(),
            linkTarget: null);

        Assert.Equal(expected, GetText(cells));
    }

    [Fact]
    public void Cursor_position_and_erase_line_are_applied()
    {
        var viewModel = new RunningJobViewModel();
        var cells = CreateCells(viewModel, "abcdef", out var cursor);

        Assert.True(viewModel.ApplyLineControl("\u001b[3G", cells, ref cursor));
        Assert.True(viewModel.ApplyLineControl("\u001b[K", cells, ref cursor));

        Assert.Equal("ab", GetText(cells));
    }

    [Fact]
    public void Erase_character_replaces_characters_with_spaces()
    {
        var viewModel = new RunningJobViewModel();
        var cells = CreateCells(viewModel, "abcdef", out var cursor);
        viewModel.ApplyLineControl("\u001b[3G", cells, ref cursor);

        Assert.True(viewModel.ApplyLineControl("\u001b[2X", cells, ref cursor));

        Assert.Equal("ab  ef", GetText(cells));
    }

    private static List<RunningJobViewModel.TerminalCell> CreateCells(
        RunningJobViewModel viewModel,
        string text,
        out int cursor)
    {
        var cells = new List<RunningJobViewModel.TerminalCell>();
        cursor = 0;
        RunningJobViewModel.WriteTerminalText(
            cells,
            ref cursor,
            text,
            viewModel.GetCurrentTextStyle(),
            linkTarget: null);
        return cells;
    }

    private static string GetText(IEnumerable<RunningJobViewModel.TerminalCell> cells) =>
        new(cells.Select(cell => cell.Character).ToArray());
}
