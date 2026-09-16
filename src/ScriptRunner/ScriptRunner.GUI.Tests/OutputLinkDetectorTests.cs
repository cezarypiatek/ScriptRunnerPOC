using ScriptRunner.GUI.Infrastructure;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class OutputLinkDetectorTests
{
    [Theory]
    [InlineData("See https://example.com/it's-valid now", "https://example.com/it's-valid")]
    [InlineData("See https://example.com/a(b)c now", "https://example.com/a(b)c")]
    [InlineData("(https://example.com/a(b))", "https://example.com/a(b)")]
    [InlineData("'https://example.com/value'", "https://example.com/value")]
    [InlineData("'https://example.com/value',", "https://example.com/value")]
    [InlineData("https://example.com/test.", "https://example.com/test")]
    public void Detect_finds_complete_url_and_excludes_surrounding_punctuation(
        string input,
        string expected)
    {
        var match = Assert.Single(OutputLinkDetector.Detect(input));

        Assert.Equal(OutputLinkKind.Url, match.Kind);
        Assert.Equal(expected, input.Substring(match.Start, match.Length));
        Assert.Equal(expected, match.Target);
    }

    [Fact]
    public void Detect_finds_existing_unquoted_path_with_spaces_before_log_message()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var file = Path.Combine(testDirectory, "file with spaces.txt");
            File.WriteAllText(file, "test");
            var input = $"Output: {file} failed validation";

            var match = Assert.Single(OutputLinkDetector.Detect(input));

            Assert.Equal(OutputLinkKind.FilePath, match.Kind);
            Assert.Equal(file, input.Substring(match.Start, match.Length));
            Assert.Equal(file, match.Target);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void Detect_finds_quoted_path_with_spaces_without_quotes()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var file = Path.Combine(testDirectory, "quoted file.txt");
            File.WriteAllText(file, "test");
            var input = $"Output: \"{file}\"";

            var match = Assert.Single(OutputLinkDetector.Detect(input));

            Assert.Equal(file, input.Substring(match.Start, match.Length));
            Assert.Equal(file, match.Target);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void Detect_excludes_source_location_suffix()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var file = Path.Combine(testDirectory, "source.cs");
            File.WriteAllText(file, "test");
            var input = $"{file}(12,4): error";

            var match = Assert.Single(OutputLinkDetector.Detect(input));

            Assert.Equal(file, input.Substring(match.Start, match.Length));
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ScriptRunnerLinkTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
