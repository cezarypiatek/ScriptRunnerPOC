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

    /// <summary>True when per-action toggles should be interactive (i.e. master switch is OFF).</summary>
    public bool CanConfigureIndividualActions => !_exposeAllActions;

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

        PopulateAvailableActions(settings.ActionOverrides);

        // Refresh the list when actions are reloaded while the dialog is open
        _mainVm.ActionsReloaded += OnActionsReloaded;
    }

    private void PopulateAvailableActions(Dictionary<string, bool> overrides)
    {
        AvailableActions.Clear();
        foreach (var action in _mainVm.Actions.OrderBy(a => a.FullName))
        {
            var enabled = overrides.TryGetValue(action.FullName, out var val) && val;
            AvailableActions.Add(new McpActionToggleViewModel
            {
                Key = action.FullName,
                DisplayName = action.FullName,
                IsEnabled = enabled
            });
        }
    }

    private void OnActionsReloaded(object? sender, EventArgs e)
    {
        // Preserve the current toggle states when refreshing after a reload
        var current = AvailableActions.ToDictionary(a => a.Key, a => a.IsEnabled);
        PopulateAvailableActions(current);
    }

    public IReadOnlyList<string> ExposedTools => _mcpHost.GetCurrentToolNames();

    public async Task SaveAndApplyAsync()
    {
        var overrides = AvailableActions.ToDictionary(a => a.Key, a => a.IsEnabled);
        var settings = new McpServerSettings
        {
            Enabled = Enabled,
            Port = Port,
            ExposeAllActions = ExposeAllActions,
            ActionOverrides = overrides
        };
        AppSettingsService.UpdateMcpServerSettings(settings);

        if (Enabled)
            await _mcpHost.StartAsync(settings);
        else
            await _mcpHost.StopAsync();

        this.RaisePropertyChanged(nameof(ExposedTools));
    }
}
