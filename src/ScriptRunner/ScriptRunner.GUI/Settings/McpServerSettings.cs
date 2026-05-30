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
}
