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

    /// <summary>
    /// When true, all exposed actions require manual user confirmation before executing.
    /// Parameters set by MCP are highlighted with an orange border and the user must click
    /// Accept or Reject instead of the action running automatically.
    /// Default is false (original auto-execute behavior).
    /// </summary>
    public bool SafeModeForAllActions { get; set; } = false;

    /// <summary>
    /// Per-action safe-mode map. Key is ScriptConfig.FullName.
    /// Only used when <see cref="SafeModeForAllActions"/> is false.
    /// Missing key means safe mode is off for the action.
    /// </summary>
    public Dictionary<string, bool> ActionSafeModeOverrides { get; set; } = new();

    /// <summary>
    /// When true, all exposed actions use fire-and-forget mode: the MCP call returns after a 3-second
    /// delay with a "running in background" message if the job has not completed by then.
    /// If the job finishes within 3 seconds the real result is returned immediately.
    /// Default is false (original blocking behavior).
    /// </summary>
    public bool FireAndForgetForAllActions { get; set; } = false;

    /// <summary>
    /// Per-action fire-and-forget map. Key is ScriptConfig.FullName.
    /// Only used when <see cref="FireAndForgetForAllActions"/> is false.
    /// Missing key means fire-and-forget is off for the action.
    /// </summary>
    public Dictionary<string, bool> ActionFireAndForgetOverrides { get; set; } = new();

    /// <summary>
    /// When true, each exposed action's predefined parameter sets (excluding the default set) are
    /// also exposed as individual MCP tools. The set-specific tools inherit all settings (output,
    /// safe mode, fire-and-forget) from their parent action.
    /// When false, only the base action tools are exposed (original behavior).
    /// </summary>
    public bool ExposePredefinedParameterSets { get; set; } = false;

    /// <summary>
    /// Per-action predefined-sets inclusion map. Key is ScriptConfig.FullName.
    /// Only consulted when <see cref="ExposePredefinedParameterSets"/> is false; in that case a
    /// value of true opts the action's sets in individually.
    /// When <see cref="ExposePredefinedParameterSets"/> is true all exposed actions include their
    /// sets and this dictionary is ignored at runtime (but values are still persisted so they are
    /// remembered if the global switch is later turned off).
    /// Missing key means the action does not include its predefined sets.
    /// </summary>
    public Dictionary<string, bool> ActionPredefinedSetsOverrides { get; set; } = new();
}
