using System.Globalization;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class DatePickerControlTests
{
    [Fact]
    public void ParsesTheConfiguredReplayFormatAndCulture()
    {
        var parsed = DatePickerControl.TryParseValue(
            "31 grudnia 2026",
            "dd MMMM yyyy",
            CultureInfo.GetCultureInfo("pl-PL"),
            out var value);

        Assert.True(parsed);
        Assert.Equal(new DateTime(2026, 12, 31), value);
    }
}
