using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ReactiveUI;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Mcp;

/// <summary>
/// Manages the embedded ASP.NET Core / Kestrel host that exposes ScriptRunner actions as MCP tools.
/// </summary>
public class ScriptRunnerMcpHost : ReactiveObject
{
    private WebApplication? _app;
    private CancellationTokenSource? _cts;
    private readonly MainWindowViewModel _vm;

    private string _statusMessage = "Stopped";
    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isRunning, value);
            _vm.IsMcpRunning = value;
        }
    }

    public ScriptRunnerMcpHost(MainWindowViewModel vm)
    {
        _vm = vm;
        // Rebuild tools whenever actions are reloaded
        _vm.ActionsReloaded += async (_, _) =>
        {
            if (_enabled && IsRunning && _lastSettings != null)
                await RestartAsync(_lastSettings);
        };
    }

    private McpServerSettings? _lastSettings;
    private bool _enabled;

    public async Task StartAsync(McpServerSettings settings)
    {
        await StopAsync();
        _enabled = true;
        _lastSettings = settings;
        try
        {
            _cts = new CancellationTokenSource();
            _app = BuildApp(settings);
            await _app.StartAsync(_cts.Token);
            IsRunning = true;
            StatusMessage = $"Running on http://127.0.0.1:{settings.Port}/mcp";
        }
        catch (Exception ex)
        {
            IsRunning = false;
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task StopAsync()
    {
        _enabled = false;
        if (_app != null)
        {
            try
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
            catch { /* ignore shutdown errors */ }
            finally
            {
                _app = null;
                _lastSettings = null;
                IsRunning = false;
                StatusMessage = "Stopped";
            }
        }
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public async Task RestartAsync(McpServerSettings settings)
    {
        await StopAsync();
        await StartAsync(settings);
    }

    private WebApplication BuildApp(McpServerSettings settings)
    {
        var bridge = new McpUiBridge(_vm);
        var allActions = bridge.GetActionsSnapshot();

        // Filter actions based on the per-tool enable/disable settings
        var exposedActions = settings.ExposeAllActions
            ? allActions
            : allActions.Where(a =>
                settings.ActionOverrides.TryGetValue(a.FullName, out var enabled) && enabled).ToArray();

        // For each exposed action, determine whether its predefined sets should also be exposed.
        // Global switch on  → all exposed actions include their sets.
        // Global switch off → check per-action override (missing key = false).
        var actionTuples = exposedActions.Select(a =>
        {
            var includeSets = settings.ExposePredefinedParameterSets
                || (settings.ActionPredefinedSetsOverrides.TryGetValue(a.FullName, out var v) && v);
            return (Action: a, IncludeSets: includeSets);
        });

        var nameMap = McpToolBuilder.BuildNameMap(actionTuples);
        var tools = nameMap.Select(t =>
        {
            // All flags are keyed by the *parent* action's FullName — predefined-set tools inherit them.
            var includeOutput = settings.ExposeOutputForAllActions
                || (settings.ActionOutputOverrides.TryGetValue(t.Action.FullName, out var v) && v);
            var safeMode = settings.SafeModeForAllActions
                || (settings.ActionSafeModeOverrides.TryGetValue(t.Action.FullName, out var sm) && sm);
            var fireAndForget = settings.FireAndForgetForAllActions
                || (settings.ActionFireAndForgetOverrides.TryGetValue(t.Action.FullName, out var ff) && ff);
            return McpToolBuilder.CreateTool(t.Action, t.ToolName, bridge, includeOutput, safeMode, fireAndForget, t.ArgumentSet);
        }).ToList();

        var builder = WebApplication.CreateSlimBuilder();

        // Bind only to loopback (DNS-rebinding protection)
        builder.WebHost.UseKestrel(k =>
        {
            k.ListenLocalhost(settings.Port);
        });

        // Suppress default host/env banners
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

        var mcpBuilder = builder.Services
            .AddMcpServer()
            .WithHttpTransport(o => { o.Stateless = true; });

        // Register each tool as a singleton McpServerTool
        foreach (var tool in tools)
        {
            mcpBuilder = mcpBuilder.WithTools(new[] { tool });
        }

        var app = builder.Build();

        // AllowedHosts: only loopback to defeat DNS rebinding
        app.UseHostFiltering();

        app.MapMcp();

        return app;
    }

    /// <summary>Returns a snapshot of the current tool names being served.</summary>
    public IReadOnlyList<string> GetCurrentToolNames()
    {
        if (!IsRunning || _lastSettings == null) return Array.Empty<string>();
        var bridge = new McpUiBridge(_vm);
        var allActions = bridge.GetActionsSnapshot();
        var exposedActions = _lastSettings.ExposeAllActions
            ? allActions
            : allActions.Where(a =>
                _lastSettings.ActionOverrides.TryGetValue(a.FullName, out var enabled) && enabled).ToArray();
        var actionTuples = exposedActions.Select(a =>
        {
            var includeSets = _lastSettings.ExposePredefinedParameterSets
                || (_lastSettings.ActionPredefinedSetsOverrides.TryGetValue(a.FullName, out var v) && v);
            return (Action: a, IncludeSets: includeSets);
        });
        return McpToolBuilder.BuildNameMap(actionTuples)
            .Select(t => t.ToolName)
            .ToList();
    }
}
