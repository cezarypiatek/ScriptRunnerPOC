using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.Mcp;

/// <summary>
/// Converts ScriptConfig actions into McpServerTool instances.
/// </summary>
public static class McpToolBuilder
{
    /// <summary>Sanitise an action FullName into a valid MCP tool name ([a-zA-Z0-9_-]+, max 64 chars).</summary>
    public static string SanitizeName(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
        }
        var result = sb.ToString().Trim('_');
        return result.Length > 64 ? result[..64] : result;
    }

    /// <summary>Build a unique set of tool names from the action list, handling collisions.</summary>
    public static IReadOnlyList<(ScriptConfig Action, string ToolName)> BuildNameMap(IEnumerable<ScriptConfig> actions)
    {
        var result = new List<(ScriptConfig, string)>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var action in actions)
        {
            var baseName = SanitizeName(action.FullName);
            if (string.IsNullOrEmpty(baseName)) baseName = "action";

            if (!seen.ContainsKey(baseName))
            {
                seen[baseName] = 1;
                result.Add((action, baseName));
            }
            else
            {
                var n = ++seen[baseName];
                var suffixed = baseName.Length <= 61 ? $"{baseName}_{n}" : $"{baseName[..61]}_{n}";
                result.Add((action, suffixed));
            }
        }

        return result;
    }

    /// <summary>
    /// Build a JSON-Schema object for the action's parameters so MCP clients can validate inputs.
    /// </summary>
    public static JsonObject BuildInputSchema(ScriptConfig action)
    {
        var props = new JsonObject();
        var required = new JsonArray();

        foreach (var p in action.Params)
        {
            var propSchema = new JsonObject();

            // Type
            switch (p.Prompt)
            {
                case PromptType.Numeric:
                    propSchema["type"] = "number";
                    break;
                case PromptType.Checkbox:
                    propSchema["type"] = "boolean";
                    break;
                case PromptType.Multiselect:
                    propSchema["type"] = "array";
                    var itemSchema = new JsonObject { ["type"] = "string" };
                    var msOptions = p.GetDropdownOptions(",");
                    if (msOptions.Count > 0)
                    {
                        var msEnum = new JsonArray();
                        foreach (var o in msOptions) msEnum.Add(o.Value);
                        itemSchema["enum"] = msEnum;
                    }
                    propSchema["items"] = itemSchema;
                    break;
                default:
                    propSchema["type"] = "string";
                    if (p.Prompt == PromptType.Dropdown)
                    {
                        var opts = p.GetDropdownOptions(",");
                        if (opts.Count > 0)
                        {
                            var enumArr = new JsonArray();
                            foreach (var o in opts) enumArr.Add(o.Value);
                            propSchema["enum"] = enumArr;
                        }
                    }
                    break;
            }

            if (!string.IsNullOrWhiteSpace(p.Description))
                propSchema["description"] = p.Description;

            if (!string.IsNullOrWhiteSpace(p.Default))
                propSchema["default"] = p.Default;

            props[p.Name] = propSchema;

            // Required if no default
            if (string.IsNullOrWhiteSpace(p.Default))
                required.Add(p.Name);
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = props
        };
        if (required.Count > 0)
            schema["required"] = required;

        return schema;
    }

    /// <summary>
    /// Create a McpServerTool for a single ScriptConfig action.
    /// </summary>
    /// <param name="action">The action to expose as a tool.</param>
    /// <param name="toolName">The sanitised MCP tool name.</param>
    /// <param name="bridge">The UI bridge used to execute the action.</param>
    /// <param name="includeOutput">
    /// When true the full visible job output (blended stdout+stderr, ANSI-stripped) is appended
    /// to the response in addition to the status line.
    /// </param>
    /// <param name="safeMode">
    /// When true, the action will not execute automatically. Instead the UI presents Accept/Reject
    /// buttons and the MCP call blocks until the user makes a choice.
    /// </param>
    /// <param name="fireAndForget">
    /// When true, the MCP call returns immediately with a "running in background" message if the
    /// job has not completed within 3 seconds of starting. If the job finishes within that window
    /// the real result is returned as normal.
    /// </param>
    public static McpServerTool CreateTool(ScriptConfig action, string toolName, McpUiBridge bridge, bool includeOutput = false, bool safeMode = false, bool fireAndForget = false)
    {
        return McpServerTool.Create(
            async (IReadOnlyDictionary<string, object?> rawArgs, CancellationToken ct) =>
            {
                // Convert raw args (object?) to string-keyed dict
                var stringArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in rawArgs)
                {
                    stringArgs[k] = v?.ToString() ?? string.Empty;
                }

                // Track which keys were explicitly supplied by the AI (before filling defaults)
                var explicitKeys = new HashSet<string>(stringArgs.Keys, StringComparer.OrdinalIgnoreCase);

                // Also fill in defaults for params not provided
                foreach (var param in action.Params)
                {
                    if (!stringArgs.ContainsKey(param.Name) && !string.IsNullOrEmpty(param.Default))
                        stringArgs[param.Name] = param.Default;
                }

                var result = await bridge.ExecuteActionAsync(action, stringArgs, ct, safeMode, explicitKeys, fireAndForget);

                if (result.Rejected)
                {
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"Action '{action.FullName}' was rejected by the user in safe mode." }
                        },
                        IsError = true
                    };
                }

                if (result.StillRunning)
                {
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"Action '{action.FullName}' was launched and is running in the background." }
                        },
                        IsError = false
                    };
                }

                var statusText = result.Success
                    ? $"Success: '{action.FullName}' completed in {result.Elapsed.TotalSeconds:F1}s"
                    : $"Failed: '{action.FullName}' finished with exit code {result.ExitCode?.ToString() ?? "unknown"} after {result.Elapsed.TotalSeconds:F1}s";

                var content = new List<ContentBlock> { new TextContentBlock { Text = statusText } };

                if (includeOutput && !string.IsNullOrEmpty(result.Output))
                {
                    content.Add(new TextContentBlock { Text = result.Output });
                }

                return new CallToolResult
                {
                    Content = content,
                    IsError = !result.Success
                };
            },
            new McpServerToolCreateOptions
            {
                Name = toolName,
                Description = !string.IsNullOrWhiteSpace(action.Description)
                    ? action.Description
                    : action.FullName,
                SerializerOptions = null
            }
        );
    }
}
