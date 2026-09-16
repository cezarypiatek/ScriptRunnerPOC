using Avalonia.Media;
using Avalonia.Media.Immutable;
using ScriptRunner.GUI.ViewModels;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class SgrFormattingTests
{
    [Fact]
    public void Applies_combined_sgr_parameters_left_to_right()
    {
        var viewModel = new RunningJobViewModel();

        viewModel.ApplyCsiSequence("\u001b[1;3;4;31;44m");
        var style = viewModel.GetCurrentTextStyle();

        Assert.True(style.IsBold);
        Assert.True(style.IsItalic);
        Assert.True(style.IsUnderline);
        Assert.Equal(Color.FromRgb(170, 0, 0), GetColor(style.Foreground));
        Assert.Equal(Color.FromRgb(0, 0, 170), GetColor(style.Background));
    }

    [Fact]
    public void Empty_sgr_and_selective_color_resets_are_supported()
    {
        var viewModel = new RunningJobViewModel();
        viewModel.ApplyCsiSequence("\u001b[91;102m");
        viewModel.ApplyCsiSequence("\u001b[39;49m");

        var selectivelyReset = viewModel.GetCurrentTextStyle();
        Assert.Equal(Brushes.White, selectivelyReset.Foreground);
        Assert.Equal(Brushes.Transparent, selectivelyReset.Background);

        viewModel.ApplyCsiSequence("\u001b[1;3;4m");
        viewModel.ApplyCsiSequence("\u001b[m");
        var fullyReset = viewModel.GetCurrentTextStyle();
        Assert.False(fullyReset.IsBold);
        Assert.False(fullyReset.IsItalic);
        Assert.False(fullyReset.IsUnderline);
    }

    [Fact]
    public void Applies_indexed_and_rgb_colors()
    {
        var viewModel = new RunningJobViewModel();

        viewModel.ApplyCsiSequence("\u001b[38;5;196;48;2;10;20;30m");
        var style = viewModel.GetCurrentTextStyle();

        Assert.Equal(Color.FromRgb(255, 0, 0), Assert.IsAssignableFrom<ISolidColorBrush>(style.Foreground).Color);
        Assert.Equal(Color.FromRgb(10, 20, 30), Assert.IsAssignableFrom<ISolidColorBrush>(style.Background).Color);
    }

    [Fact]
    public void Applies_colon_rgb_form_with_omitted_color_space()
    {
        var viewModel = new RunningJobViewModel();

        viewModel.ApplyCsiSequence("\u001b[38:2::12:34:56m");

        var foreground = Assert.IsAssignableFrom<ISolidColorBrush>(viewModel.GetCurrentTextStyle().Foreground);
        Assert.Equal(Color.FromRgb(12, 34, 56), foreground.Color);
    }

    [Fact]
    public void Inverse_is_idempotent_and_27_disables_it()
    {
        var viewModel = new RunningJobViewModel();
        viewModel.ApplyCsiSequence("\u001b[31;44m");

        viewModel.ApplyCsiSequence("\u001b[7;7m");
        var inverted = viewModel.GetCurrentTextStyle();
        Assert.Equal(Color.FromRgb(0, 0, 170), GetColor(inverted.Foreground));
        Assert.Equal(Color.FromRgb(170, 0, 0), GetColor(inverted.Background));

        viewModel.ApplyCsiSequence("\u001b[27m");
        var normal = viewModel.GetCurrentTextStyle();
        Assert.Equal(Color.FromRgb(170, 0, 0), GetColor(normal.Foreground));
        Assert.Equal(Color.FromRgb(0, 0, 170), GetColor(normal.Background));
    }

    [Fact]
    public void Applies_and_resets_additional_attributes()
    {
        var viewModel = new RunningJobViewModel();
        viewModel.ApplyCsiSequence("\u001b[2;8;21;53;58;2;1;2;3m");

        var active = viewModel.GetCurrentTextStyle();
        Assert.True(active.IsDoubleUnderline);
        Assert.True(active.IsOverline);
        Assert.Equal(Brushes.Transparent, active.Foreground);
        Assert.NotNull(active.UnderlineColor);

        viewModel.ApplyCsiSequence("\u001b[22;24;28;55;59m");
        var reset = viewModel.GetCurrentTextStyle();
        Assert.False(reset.IsDoubleUnderline);
        Assert.False(reset.IsOverline);
        Assert.Null(reset.UnderlineColor);
    }

    [Fact]
    public async Task Dynamic_sgr_brushes_are_safe_to_create_on_a_worker_thread()
    {
        var styles = await Task.Run(() =>
        {
            var viewModel = new RunningJobViewModel();
            viewModel.ApplyCsiSequence("\u001b[2;38;2;12;34;56;48;5;208;58;2;1;2;3m");
            return viewModel.GetCurrentTextStyle();
        });

        Assert.IsType<ImmutableSolidColorBrush>(styles.Foreground);
        Assert.IsType<ImmutableSolidColorBrush>(styles.Background);
        Assert.IsType<ImmutableSolidColorBrush>(styles.UnderlineColor);
    }

    [Theory]
    [InlineData(30, 0, 0, 0)]
    [InlineData(31, 170, 0, 0)]
    [InlineData(32, 0, 170, 0)]
    [InlineData(33, 170, 85, 0)]
    [InlineData(34, 0, 0, 170)]
    [InlineData(35, 170, 0, 170)]
    [InlineData(36, 0, 170, 170)]
    [InlineData(37, 170, 170, 170)]
    [InlineData(90, 85, 85, 85)]
    [InlineData(91, 255, 85, 85)]
    [InlineData(92, 85, 255, 85)]
    [InlineData(93, 255, 255, 85)]
    [InlineData(94, 85, 85, 255)]
    [InlineData(95, 255, 85, 255)]
    [InlineData(96, 85, 255, 255)]
    [InlineData(97, 255, 255, 255)]
    public void Uses_consistent_classic_ansi_palette(int code, int red, int green, int blue)
    {
        var viewModel = new RunningJobViewModel();

        viewModel.ApplyCsiSequence($"\u001b[{code}m");

        Assert.Equal(
            Color.FromRgb((byte)red, (byte)green, (byte)blue),
            GetColor(viewModel.GetCurrentTextStyle().Foreground));
    }

    private static Color GetColor(IBrush brush) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
}
