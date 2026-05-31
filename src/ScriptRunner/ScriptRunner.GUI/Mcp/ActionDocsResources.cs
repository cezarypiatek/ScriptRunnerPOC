using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ScriptRunner.GUI.Mcp;

[McpServerResourceType]
public class ActionDocsResources
{
    private static readonly ConcurrentDictionary<string, string> DocsByToolName = new(StringComparer.Ordinal);

    public static void SetDocs(IReadOnlyDictionary<string, string> docs)
    {
        DocsByToolName.Clear();
        foreach (var (toolName, content) in docs)
        {
            DocsByToolName[toolName] = content;
        }
    }

    public static string CreateUri(string toolName) => $"scriptrunner://actions/{toolName}/docs";

    public static string CreateHttpPath(string toolName) => $"/actions/{toolName}/docs";

    public static bool TryGetDocs(string toolName, out string content)
    {
        toolName = toolName.Trim();
        return DocsByToolName.TryGetValue(toolName, out content!);
    }

    [McpServerResource(UriTemplate = "scriptrunner://actions/{tool}/docs", Name = "Action docs", MimeType = "text/markdown")]
    [Description("Returns documentation for a ScriptRunner MCP action tool")]
    public static TextResourceContents GetActionDocs(string tool)
    {
        tool = tool.Trim();
        if (!TryGetDocs(tool, out var content))
        {
            throw new InvalidOperationException($"No docs resource found for tool '{tool}'.");
        }

        return new TextResourceContents
        {
            Uri = CreateUri(tool),
            MimeType = "text/markdown",
            Text = content
        };
    }
}
