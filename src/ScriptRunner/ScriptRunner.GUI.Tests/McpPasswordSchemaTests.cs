using ScriptRunner.GUI.Mcp;
using ScriptRunner.GUI.ScriptConfigs;
using Xunit;

namespace ScriptRunner.GUI.Tests;

public class McpPasswordSchemaTests
{
    [Fact]
    public void PasswordDefaultIsNotPublishedInMcpSchema()
    {
        var action = new ScriptConfig
        {
            Name = "Action",
            Params =
            {
                new ScriptParam
                {
                    Name = "token",
                    Prompt = PromptType.Password,
                    Default = "plaintext-secret"
                }
            }
        };

        var schema = McpToolBuilder.BuildInputSchema(action);
        var passwordSchema = schema["properties"]!["token"]!;

        Assert.Null(passwordSchema["default"]);
    }
}
