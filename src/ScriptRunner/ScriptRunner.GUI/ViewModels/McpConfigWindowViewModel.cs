using System;
using System.Collections.Generic;
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

    public string StatusMessage => _mcpHost.StatusMessage;
    public bool IsRunning => _mcpHost.IsRunning;

    public string EndpointUrl => $"http://127.0.0.1:{Port}/mcp";

    public McpConfigWindowViewModel() : this(Locator.Current.GetService<ScriptRunnerMcpHost>()
        ?? throw new InvalidOperationException("ScriptRunnerMcpHost not registered"))
    {
    }

    public McpConfigWindowViewModel(ScriptRunnerMcpHost mcpHost)
    {
        _mcpHost = mcpHost;

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
    }

    public IReadOnlyList<string> ExposedTools => _mcpHost.GetCurrentToolNames();

    public async Task SaveAndApplyAsync()
    {
        var settings = new McpServerSettings { Enabled = Enabled, Port = Port };
        AppSettingsService.UpdateMcpServerSettings(settings);

        if (Enabled)
            await _mcpHost.StartAsync(settings);
        else
            await _mcpHost.StopAsync();

        this.RaisePropertyChanged(nameof(ExposedTools));
    }
}
