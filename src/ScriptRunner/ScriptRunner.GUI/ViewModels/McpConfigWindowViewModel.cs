using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ScriptRunner.GUI.Mcp;
using ScriptRunner.GUI.Settings;
using Splat;

namespace ScriptRunner.GUI.ViewModels;

public class McpConfigWindowViewModel : ViewModelBase
{
    private readonly ScriptRunnerMcpHost _mcpHost;
    private readonly MainWindowViewModel _mainVm;

    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    private int _port;
    public int Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }

    private bool _exposeAllActions;
    public bool ExposeAllActions
    {
        get => _exposeAllActions;
        set
        {
            this.RaiseAndSetIfChanged(ref _exposeAllActions, value);
            this.RaisePropertyChanged(nameof(CanConfigureIndividualActions));
        }
    }

    /// <summary>True when per-action availability toggles should be interactive (i.e. master switch is OFF).</summary>
    public bool CanConfigureIndividualActions => !_exposeAllActions;

    private bool _exposeOutputForAllActions;
    public bool ExposeOutputForAllActions
    {
        get => _exposeOutputForAllActions;
        set
        {
            this.RaiseAndSetIfChanged(ref _exposeOutputForAllActions, value);
            this.RaisePropertyChanged(nameof(CanConfigureIndividualOutput));
            // Push updated value to each row so CanEditExposeOutput updates immediately
            foreach (var row in AvailableActions)
                row.CanConfigureIndividualOutput = !value;
        }
    }

    /// <summary>True when per-action output toggles should be interactive (i.e. output master switch is OFF).</summary>
    public bool CanConfigureIndividualOutput => !_exposeOutputForAllActions;

    private bool _safeModeForAllActions;
    public bool SafeModeForAllActions
    {
        get => _safeModeForAllActions;
        set
        {
            this.RaiseAndSetIfChanged(ref _safeModeForAllActions, value);
            this.RaisePropertyChanged(nameof(CanConfigureIndividualSafeMode));
            // Push updated value to each row so CanEditSafeMode updates immediately
            foreach (var row in AvailableActions)
                row.CanConfigureIndividualSafeMode = !value;
        }
    }

    /// <summary>True when per-action safe mode toggles should be interactive (i.e. safe mode master switch is OFF).</summary>
    public bool CanConfigureIndividualSafeMode => !_safeModeForAllActions;

    private bool _fireAndForgetForAllActions;
    public bool FireAndForgetForAllActions
    {
        get => _fireAndForgetForAllActions;
        set
        {
            this.RaiseAndSetIfChanged(ref _fireAndForgetForAllActions, value);
            this.RaisePropertyChanged(nameof(CanConfigureIndividualFireAndForget));
            // Push updated value to each row so CanEditFireAndForget updates immediately
            foreach (var row in AvailableActions)
                row.CanConfigureIndividualFireAndForget = !value;
        }
    }

    /// <summary>True when per-action fire-and-forget toggles should be interactive (i.e. fire-and-forget master switch is OFF).</summary>
    public bool CanConfigureIndividualFireAndForget => !_fireAndForgetForAllActions;

    private bool _exposePredefinedParameterSets;
    public bool ExposePredefinedParameterSets
    {
        get => _exposePredefinedParameterSets;
        set
        {
            this.RaiseAndSetIfChanged(ref _exposePredefinedParameterSets, value);
            this.RaisePropertyChanged(nameof(CanConfigureIndividualPredefinedSets));
            // Push updated value to each row so CanEditIncludePredefinedSets updates immediately.
            // When global is ON → per-action editing is disabled (all sets included automatically).
            // When global is OFF → per-action editing is enabled.
            foreach (var row in AvailableActions)
                row.CanConfigureIndividualPredefinedSets = !value;
        }
    }

    /// <summary>True when per-action predefined-sets toggles should be interactive (i.e. global predefined-sets switch is OFF).</summary>
    public bool CanConfigureIndividualPredefinedSets => !_exposePredefinedParameterSets;

    public ObservableCollection<McpActionToggleViewModel> AvailableActions { get; } = new();

    public string StatusMessage => _mcpHost.StatusMessage;
    public bool IsRunning => _mcpHost.IsRunning;

    public string EndpointUrl => $"http://127.0.0.1:{Port}/mcp";

    public McpConfigWindowViewModel() : this(
        Locator.Current.GetService<ScriptRunnerMcpHost>()
            ?? throw new InvalidOperationException("ScriptRunnerMcpHost not registered"),
        Locator.Current.GetService<MainWindowViewModel>()
            ?? throw new InvalidOperationException("MainWindowViewModel not registered"))
    {
    }

    public McpConfigWindowViewModel(ScriptRunnerMcpHost mcpHost, MainWindowViewModel mainVm)
    {
        _mcpHost = mcpHost;
        _mainVm = mainVm;

        // Subscribe to host status changes
        _mcpHost.WhenAnyValue(x => x.StatusMessage)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(StatusMessage)));
        _mcpHost.WhenAnyValue(x => x.IsRunning)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(IsRunning));
                this.RaisePropertyChanged(nameof(EndpointUrl));
            });

        var settings = AppSettingsService.Load().McpServer;
        _enabled = settings.Enabled;
        _port = settings.Port;
        _exposeAllActions = settings.ExposeAllActions;
        _exposeOutputForAllActions = settings.ExposeOutputForAllActions;
        _safeModeForAllActions = settings.SafeModeForAllActions;
        _fireAndForgetForAllActions = settings.FireAndForgetForAllActions;
        _exposePredefinedParameterSets = settings.ExposePredefinedParameterSets;

        PopulateAvailableActions(settings.ActionOverrides, settings.ActionOutputOverrides, settings.ActionSafeModeOverrides, settings.ActionFireAndForgetOverrides, settings.ActionPredefinedSetsOverrides);

        // Refresh the list when actions are reloaded while the dialog is open
        _mainVm.ActionsReloaded += OnActionsReloaded;
    }

    private void PopulateAvailableActions(
        Dictionary<string, bool> overrides,
        Dictionary<string, bool>? outputOverrides = null,
        Dictionary<string, bool>? safeModeOverrides = null,
        Dictionary<string, bool>? fireAndForgetOverrides = null,
        Dictionary<string, bool>? predefinedSetsOverrides = null)
    {
        AvailableActions.Clear();
        foreach (var action in _mainVm.Actions.OrderBy(a => a.FullName))
        {
            var enabled = overrides.TryGetValue(action.FullName, out var val) && val;
            var exposeOutput = outputOverrides != null && outputOverrides.TryGetValue(action.FullName, out var outVal) && outVal;
            var safeMode = safeModeOverrides != null && safeModeOverrides.TryGetValue(action.FullName, out var smVal) && smVal;
            var fireAndForget = fireAndForgetOverrides != null && fireAndForgetOverrides.TryGetValue(action.FullName, out var ffVal) && ffVal;
            var includePredefinedSets = predefinedSetsOverrides != null && predefinedSetsOverrides.TryGetValue(action.FullName, out var psVal) && psVal;
            var hasPredefinedSets = action.PredefinedArgumentSets.Any(s => s.Description != "<default>");
            AvailableActions.Add(new McpActionToggleViewModel
            {
                Key = action.FullName,
                DisplayName = action.FullName,
                HasPredefinedSets = hasPredefinedSets,
                IsEnabled = enabled,
                ExposeOutput = exposeOutput,
                CanConfigureIndividualOutput = !_exposeOutputForAllActions,
                SafeMode = safeMode,
                CanConfigureIndividualSafeMode = !_safeModeForAllActions,
                FireAndForget = fireAndForget,
                CanConfigureIndividualFireAndForget = !_fireAndForgetForAllActions,
                IncludePredefinedSets = includePredefinedSets,
                CanConfigureIndividualPredefinedSets = !_exposePredefinedParameterSets
            });
        }
    }

    private void OnActionsReloaded(object? sender, EventArgs e)
    {
        // Preserve the current toggle states when refreshing after a reload
        var current = AvailableActions.ToDictionary(a => a.Key, a => a.IsEnabled);
        var currentOutput = AvailableActions.ToDictionary(a => a.Key, a => a.ExposeOutput);
        var currentSafeMode = AvailableActions.ToDictionary(a => a.Key, a => a.SafeMode);
        var currentFireAndForget = AvailableActions.ToDictionary(a => a.Key, a => a.FireAndForget);
        var currentPredefinedSets = AvailableActions.ToDictionary(a => a.Key, a => a.IncludePredefinedSets);
        PopulateAvailableActions(current, currentOutput, currentSafeMode, currentFireAndForget, currentPredefinedSets);
    }

    public IReadOnlyList<string> ExposedTools => _mcpHost.GetCurrentToolNames();

    public async Task SaveAndApplyAsync()
    {
        var overrides = AvailableActions.ToDictionary(a => a.Key, a => a.IsEnabled);
        var outputOverrides = AvailableActions.ToDictionary(a => a.Key, a => a.ExposeOutput);
        var safeModeOverrides = AvailableActions.ToDictionary(a => a.Key, a => a.SafeMode);
        var fireAndForgetOverrides = AvailableActions.ToDictionary(a => a.Key, a => a.FireAndForget);
        var predefinedSetsOverrides = AvailableActions.ToDictionary(a => a.Key, a => a.IncludePredefinedSets);
        var settings = new McpServerSettings
        {
            Enabled = Enabled,
            Port = Port,
            ExposeAllActions = ExposeAllActions,
            ActionOverrides = overrides,
            ExposeOutputForAllActions = ExposeOutputForAllActions,
            ActionOutputOverrides = outputOverrides,
            SafeModeForAllActions = SafeModeForAllActions,
            ActionSafeModeOverrides = safeModeOverrides,
            FireAndForgetForAllActions = FireAndForgetForAllActions,
            ActionFireAndForgetOverrides = fireAndForgetOverrides,
            ExposePredefinedParameterSets = ExposePredefinedParameterSets,
            ActionPredefinedSetsOverrides = predefinedSetsOverrides
        };
        AppSettingsService.UpdateMcpServerSettings(settings);

        if (Enabled)
            await _mcpHost.StartAsync(settings);
        else
            await _mcpHost.StopAsync();

        this.RaisePropertyChanged(nameof(ExposedTools));
    }
}
