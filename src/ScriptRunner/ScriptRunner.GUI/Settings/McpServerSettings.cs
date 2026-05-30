using System.Collections.Generic;

namespace ScriptRunner.GUI.Settings;

public class McpServerSettings
{
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 3001;

    /// <summary>
    /// When true, all loaded actions are exposed as MCP tools (original behavior).
    /// When false, only actions present with value true in <see cref="ActionOverrides"/> are exposed.
    /// </summary>
    public bool ExposeAllActions { get; set; } = true;

    /// <summary>
    /// Per-action enable/disable map. Key is ScriptConfig.FullName.
    /// Only used when <see cref="ExposeAllActions"/> is false.
    /// Missing key means the action is disabled.
    /// </summary>
    public Dictionary<string, bool> ActionOverrides { get; set; } = new();

    /// <summary>
    /// When true, all exposed actions return their full visible output in the MCP response.
    /// When false, only actions present with value true in <see cref="ActionOutputOverrides"/> return output.
    /// Default is false (status-only, preserving original behavior).
    /// </summary>
    public bool ExposeOutputForAllActions { get; set; } = false;

    /// <summary>
    /// Per-action output-exposure map. Key is ScriptConfig.FullName.
    /// Only used when <see cref="ExposeOutputForAllActions"/> is false.
    /// Missing key means the action returns status only.
    /// </summary>
    public Dictionary<string, bool> ActionOutputOverrides { get; set; } = new();
}
