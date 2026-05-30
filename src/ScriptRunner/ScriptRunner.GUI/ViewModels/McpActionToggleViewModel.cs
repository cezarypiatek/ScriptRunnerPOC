using ReactiveUI;

namespace ScriptRunner.GUI.ViewModels;

/// <summary>
/// Represents a single action row in the MCP config tool list.
/// </summary>
public class McpActionToggleViewModel : ReactiveObject
{
    /// <summary>ScriptConfig.FullName — used as the persistence key.</summary>
    public string Key { get; init; } = "";

    /// <summary>Human-readable label shown in the grid.</summary>
    public string DisplayName { get; init; } = "";

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }
}
