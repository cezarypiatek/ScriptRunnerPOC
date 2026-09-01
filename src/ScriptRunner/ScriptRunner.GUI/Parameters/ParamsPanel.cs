using System.Collections.Generic;
using Avalonia.Controls;


namespace ScriptRunner.GUI;

public class ParamsPanel
{
    public Panel Panel { get; set; }

    public IEnumerable<IControlRecord> ControlRecords { get; set; }

    /// <summary>
    /// Maps parameter name → the wrapping Border around that parameter's row.
    /// Used to apply/clear the orange MCP-modified highlight.
    /// </summary>
    public Dictionary<string, Border> ParameterContainers { get; set; } = new();

    /// <summary>
    /// Maps parameter names to their group tabs so MCP-modified groups can be indicated.
    /// </summary>
    public Dictionary<string, TabItem> ParameterGroupTabs { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
}

