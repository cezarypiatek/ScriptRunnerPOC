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
            this.RaisePropertyChanged(nameof(CanEditSafeMode));
            this.RaisePropertyChanged(nameof(CanEditFireAndForget));
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

    private bool _safeMode;
    /// <summary>When true, MCP calls to this action require manual user confirmation before executing.</summary>
    public bool SafeMode
    {
        get => _safeMode;
        set => this.RaiseAndSetIfChanged(ref _safeMode, value);
    }

    private bool _canConfigureIndividualSafeMode;
    /// <summary>
    /// Pushed from the parent VM whenever <see cref="McpConfigWindowViewModel.CanConfigureIndividualSafeMode"/> changes.
    /// Combines with <see cref="IsEnabled"/> to produce <see cref="CanEditSafeMode"/>.
    /// </summary>
    public bool CanConfigureIndividualSafeMode
    {
        get => _canConfigureIndividualSafeMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _canConfigureIndividualSafeMode, value);
            this.RaisePropertyChanged(nameof(CanEditSafeMode));
        }
    }

    /// <summary>
    /// True when the "Safe mode" toggle for this row should be interactive:
    /// the action must be exposed AND the master safe mode switch must be OFF.
    /// </summary>
    public bool CanEditSafeMode => _isEnabled && _canConfigureIndividualSafeMode;

    private bool _fireAndForget;
    /// <summary>When true, the MCP call returns after 3 seconds with a background-running notice if the job hasn't finished yet.</summary>
    public bool FireAndForget
    {
        get => _fireAndForget;
        set => this.RaiseAndSetIfChanged(ref _fireAndForget, value);
    }

    private bool _canConfigureIndividualFireAndForget;
    /// <summary>
    /// Pushed from the parent VM whenever <see cref="McpConfigWindowViewModel.CanConfigureIndividualFireAndForget"/> changes.
    /// Combines with <see cref="IsEnabled"/> to produce <see cref="CanEditFireAndForget"/>.
    /// </summary>
    public bool CanConfigureIndividualFireAndForget
    {
        get => _canConfigureIndividualFireAndForget;
        set
        {
            this.RaiseAndSetIfChanged(ref _canConfigureIndividualFireAndForget, value);
            this.RaisePropertyChanged(nameof(CanEditFireAndForget));
        }
    }

    /// <summary>
    /// True when the "Fire &amp; forget" toggle for this row should be interactive:
    /// the action must be exposed AND the master fire-and-forget switch must be OFF.
    /// </summary>
    public bool CanEditFireAndForget => _isEnabled && _canConfigureIndividualFireAndForget;
}
