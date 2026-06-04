using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.Mcp;

/// <summary>
/// Describes a single MCP tool target — either a base action or a predefined parameter set
/// variant of an action.
/// </summary>
/// <param name="Action">The parent action.</param>
/// <param name="ArgumentSet">
/// The predefined argument set this tool represents, or <c>null</c> for the base action tool.
/// </param>
/// <param name="ToolName">The sanitised, deduplicated MCP tool name.</param>
public record ToolTarget(ScriptConfig Action, ArgumentSet? ArgumentSet, string ToolName);

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

    /// <summary>
    /// Build a unique set of tool targets from the action list, handling collisions.
    /// </summary>
    /// <param name="actions">
    /// Sequence of <c>(ScriptConfig action, bool includeSets)</c> tuples. When
    /// <c>includeSets</c> is <c>true</c> the action's non-default predefined argument sets are
    /// each emitted as an additional <see cref="ToolTarget"/>.
    /// </param>
    public static IReadOnlyList<ToolTarget> BuildNameMap(IEnumerable<(ScriptConfig Action, bool IncludeSets)> actions)
    {
        var result = new List<ToolTarget>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        string Deduplicate(string baseName)
        {
            if (!seen.ContainsKey(baseName))
            {
                seen[baseName] = 1;
                return baseName;
            }
            var n = ++seen[baseName];
            // Reserve 3 chars for "_NN" suffix (supports up to _99 cleanly, more if needed)
            var trimmed = baseName.Length <= 61 ? baseName : baseName[..61];
            return $"{trimmed}_{n}";
        }

        foreach (var (action, includeSets) in actions)
        {
            // Base action tool
            var baseName = SanitizeName(action.FullName);
            if (string.IsNullOrEmpty(baseName)) baseName = "action";
            result.Add(new ToolTarget(action, null, Deduplicate(baseName)));

            // Predefined set tools
            if (!includeSets) continue;
            foreach (var set in action.PredefinedArgumentSets)
            {
                if (set.Description == "<default>") continue;
                var setRaw = $"{action.FullName}__{set.Description}";
                var setBase = SanitizeName(setRaw);
                if (string.IsNullOrEmpty(setBase)) setBase = "action_set";
                result.Add(new ToolTarget(action, set, Deduplicate(setBase)));
            }
        }

        return result;
    }

    /// <summary>
    /// Build a JSON-Schema object for the action's parameters so MCP clients can validate inputs.
    /// </summary>
    /// <param name="action">The parent action.</param>
    /// <param name="argumentSet">
    /// Optional predefined argument set. When provided, values from the set are used as
    /// <c>default</c> for the corresponding parameters and those parameters are not listed as
    /// <c>required</c> (since the set already supplies them).
    /// </param>
    public static JsonObject BuildInputSchema(ScriptConfig action, ArgumentSet? argumentSet = null)
    {
        var props = new JsonObject();
        var required = new JsonArray();

        foreach (var p in action.Params)
        {
            var propSchema = new JsonObject();
            
            // Effective default: set value takes priority over param default
            var effectiveDefault = argumentSet != null
                                   && argumentSet.Arguments.TryGetValue(p.Name, out var setVal)
                                   && !string.IsNullOrEmpty(setVal)
                ? setVal
                : p.Default;

            if (!string.IsNullOrWhiteSpace(effectiveDefault))
            {
                propSchema["default"] = effectiveDefault;
            }
            

            // Type
            switch (p.Prompt)
            {
                case PromptType.Numeric:
                    propSchema["type"] = "number";
                    break;
                case PromptType.Checkbox:
                    propSchema["type"] = "boolean";
                    if (string.IsNullOrWhiteSpace(effectiveDefault) == false &&
                        p.GetPromptSettings("checkedValue", out var trueDefault))
                    {
                        propSchema["default"] = JsonValue.Create(trueDefault == effectiveDefault);    
                    }
                    else
                    {
                        propSchema["default"] = JsonValue.Create(false);    
                    }
                    break;
                case PromptType.Multiselect:
                    propSchema["type"] = "array";
                    var itemSchema = new JsonObject { ["type"] = "string" };
                    var delimiterForMulti = p.GetPromptSettings("delimiter", s => s, ",");
                    var msOptions = p.GetDropdownOptions(delimiterForMulti);
                    if (msOptions.Count > 0)
                    {
                        var msEnum = new JsonArray();
                        foreach (var o in msOptions) msEnum.Add(o.Value);
                        itemSchema["enum"] = msEnum;
                    }
                    propSchema["items"] = itemSchema;

                    if (string.IsNullOrWhiteSpace(effectiveDefault) == false)
                    {
                        var defaultOptions = new JsonArray();
                        foreach (var o in effectiveDefault.Split(delimiterForMulti))
                        {
                            defaultOptions.Add(o);
                        }
                        propSchema["default"] = defaultOptions;
                    }
                    
                    break;
                default:
                    propSchema["type"] = "string";
                    if (p.Prompt == PromptType.Dropdown)
                    {
                        var delimiter = p.GetPromptSettings("delimiter", s => s, ",");
                        var opts = p.GetDropdownOptions(delimiter);
                        if (opts.Count > 0)
                        {
                            var enumArr = new JsonArray();
                            foreach (var o in opts) enumArr.Add(o.Value);
                            propSchema["enum"] = enumArr;
                        }
                    }
                    break;
            }

            var parameterDescription = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim();
            if (string.IsNullOrWhiteSpace(p.Details) == false)
            {
                parameterDescription = string.IsNullOrWhiteSpace(parameterDescription)
                    ? p.Details.Trim()
                    : $"{parameterDescription}\n\nDetails: {p.Details.Trim()}";
            }

            if (string.IsNullOrWhiteSpace(parameterDescription) == false)
                propSchema["description"] = parameterDescription;

            

            props[p.Name] = propSchema;

            // Booleans are never required — false is always a valid implicit default.
            // Passwords are never reported as required — MCP clients cannot securely supply them.
            // All other params are required only when no default is available from either source.
            if (p.Prompt != PromptType.Checkbox && p.Prompt != PromptType.Password && string.IsNullOrWhiteSpace(effectiveDefault))
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
    /// Create a McpServerTool for a single ScriptConfig action (or a predefined parameter set
    /// variant of it).
    /// </summary>
    /// <param name="action">The action to expose as a tool.</param>
    /// <param name="toolName">The sanitised MCP tool name.</param>
    /// <param name="bridge">The UI bridge used to execute the action.</param>
    /// <param name="includeOutput">
    /// When true the full visible job output (blended stdout+stderr, ANSI-stripped) is appended
    /// to the response in addition to the status line.
    /// </param>
    /// <param name="braveMode">
    /// When true, the action executes immediately without manual approval.
    /// When false, the UI presents Accept/Reject buttons and the MCP call blocks until the user
    /// makes a choice.
    /// </param>
    /// <param name="fireAndForget">
    /// When true, the MCP call returns immediately with a "running in background" message if the
    /// job has not completed within 3 seconds of starting. If the job finishes within that window
    /// the real result is returned as normal.
    /// </param>
    /// <param name="argumentSet">
    /// Optional predefined argument set. When provided the tool pre-selects that set before
    /// applying any caller-supplied arguments, so the set's values act as defaults that the
    /// caller can override.
    /// </param>
    public static McpServerTool CreateTool(
        ScriptConfig action,
        string toolName,
        string? docsResourceUri,
        string? docsHttpUri,
        McpUiBridge bridge,
        bool includeOutput = false,
        bool braveMode = false,
        bool fireAndForget = false,
        ArgumentSet? argumentSet = null)
    {
        var description = !string.IsNullOrWhiteSpace(action.Description)
            ? action.Description
            : action.FullName;
        if (argumentSet != null)
            description = $"{description} [parameter set: {argumentSet.Description}]";
        if (!string.IsNullOrWhiteSpace(docsResourceUri))
            description = $"{description} More details (resource): {docsResourceUri}";
        if (!string.IsNullOrWhiteSpace(docsHttpUri))
            description = $"{description} More details (http): {docsHttpUri}";

        var schemaJson = BuildInputSchema(action, argumentSet).ToJsonString();
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var function = new DynamicScriptAIFunction(
            toolName,
            description,
            schema,
            async (AIFunctionArguments args, CancellationToken ct) =>
            {
                // Convert raw args (object?) to string-keyed dict
                var stringArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in args)
                {
                    if (v is JsonElement { ValueKind: JsonValueKind.String } stringValue)
                    {
                        var rawString = stringValue.GetString();
                        stringArgs[k] = rawString ?? string.Empty;
                    }
                    else
                    {
                        stringArgs[k] = v?.ToString() ?? string.Empty;
                    }
                }

                // Track which keys were explicitly supplied by the AI (before filling defaults).
                // Pre-filled set values are NOT included — they are treated as approved defaults
                // and should not be highlighted when approval is required.
                var explicitKeys = new HashSet<string>(stringArgs.Keys, StringComparer.OrdinalIgnoreCase);

                // Fill in defaults for params not provided by the caller.
                // For set-based tools, set values take priority over param defaults.
                foreach (var param in action.Params)
                {
                    if (stringArgs.ContainsKey(param.Name)) continue;

                    var setDefault = argumentSet != null
                        && argumentSet.Arguments.TryGetValue(param.Name, out var sv)
                        && !string.IsNullOrEmpty(sv)
                        ? sv : null;

                    var effectiveDefault = setDefault ?? (string.IsNullOrEmpty(param.Default) ? null : param.Default);
                    if (effectiveDefault != null)
                        stringArgs[param.Name] = effectiveDefault;
                }

                var result = await bridge.ExecuteActionAsync(action, stringArgs, ct, braveMode, explicitKeys, fireAndForget, argumentSet);

                if (result.Rejected)
                {
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"Action '{action.FullName}' was rejected by the user during approval." }
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

                return (object)new CallToolResult
                {
                    Content = content,
                    IsError = !result.Success
                };
            });

        return McpServerTool.Create(function, new McpServerToolCreateOptions
        {
            Name = toolName,
            Description = description,
            SerializerOptions = null
        });
    }

    /// <summary>
    /// An <see cref="AIFunction"/> implementation with a pre-built JSON Schema, used to expose
    /// dynamically-defined ScriptConfig parameters as properly-typed MCP tool inputs.
    /// </summary>
    private sealed class DynamicScriptAIFunction : AIFunction
    {
        private readonly string _name;
        private readonly string _description;
        private readonly JsonElement _schema;
        private readonly Func<AIFunctionArguments, CancellationToken, ValueTask<object?>> _invoke;

        public DynamicScriptAIFunction(
            string name,
            string description,
            JsonElement schema,
            Func<AIFunctionArguments, CancellationToken, ValueTask<object?>> invoke)
        {
            _name = name;
            _description = description;
            _schema = schema;
            _invoke = invoke;
        }

        public override string Name => _name;
        public override string Description => _description;
        public override JsonElement JsonSchema => _schema;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments, CancellationToken cancellationToken)
            => _invoke(arguments, cancellationToken);
    }
}
