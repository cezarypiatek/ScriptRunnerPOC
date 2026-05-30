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
        set
        {
            this.RaiseAndSetIfChanged(ref _isEnabled, value);
            this.RaisePropertyChanged(nameof(CanEditExposeOutput));
        }
    }

    private bool _exposeOutput;
    /// <summary>When true, the full visible job output is returned in the MCP response for this action.</summary>
    public bool ExposeOutput
    {
        get => _exposeOutput;
        set => this.RaiseAndSetIfChanged(ref _exposeOutput, value);
    }

    /// <summary>
    /// Pushed from the parent VM whenever <see cref="McpConfigWindowViewModel.CanConfigureIndividualOutput"/> changes.
    /// Combines with <see cref="IsEnabled"/> to produce <see cref="CanEditExposeOutput"/>.
    /// </summary>
    private bool _canConfigureIndividualOutput;
    public bool CanConfigureIndividualOutput
    {
        get => _canConfigureIndividualOutput;
        set
        {
            this.RaiseAndSetIfChanged(ref _canConfigureIndividualOutput, value);
            this.RaisePropertyChanged(nameof(CanEditExposeOutput));
        }
    }

    /// <summary>
    /// True when the "Return output" toggle for this row should be interactive:
    /// the action must be exposed AND the master output switch must be OFF.
    /// </summary>
    public bool CanEditExposeOutput => _isEnabled && _canConfigureIndividualOutput;
}
