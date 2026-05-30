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

    /// <summary>
    /// True when the action has at least one predefined argument set (excluding the synthetic
    /// &lt;default&gt; set). Used to show/hide the "Param sets" toggle column for this row.
    /// </summary>
    public bool HasPredefinedSets { get; init; }

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
            this.RaisePropertyChanged(nameof(CanEditIncludePredefinedSets));
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

    private bool _includePredefinedSets;
    /// <summary>
    /// When true, this action's predefined parameter sets (excluding &lt;default&gt;) are exposed
    /// as individual MCP tools in addition to the base action tool.
    /// Only relevant when <see cref="HasPredefinedSets"/> is true.
    /// </summary>
    public bool IncludePredefinedSets
    {
        get => _includePredefinedSets;
        set => this.RaiseAndSetIfChanged(ref _includePredefinedSets, value);
    }

    private bool _canConfigureIndividualPredefinedSets;
    /// <summary>
    /// Pushed from the parent VM whenever
    /// <see cref="McpConfigWindowViewModel.CanConfigureIndividualPredefinedSets"/> changes.
    /// When false (global predefined-sets switch is ON) the per-action toggle is disabled
    /// because all sets are included automatically.
    /// </summary>
    public bool CanConfigureIndividualPredefinedSets
    {
        get => _canConfigureIndividualPredefinedSets;
        set
        {
            this.RaiseAndSetIfChanged(ref _canConfigureIndividualPredefinedSets, value);
            this.RaisePropertyChanged(nameof(CanEditIncludePredefinedSets));
        }
    }

    /// <summary>
    /// True when the "Param sets" toggle for this row should be interactive:
    /// the action must be exposed, the global predefined-sets switch must be OFF, and the
    /// action must actually have non-default predefined sets.
    /// </summary>
    public bool CanEditIncludePredefinedSets => _isEnabled && _canConfigureIndividualPredefinedSets && HasPredefinedSets;
}
