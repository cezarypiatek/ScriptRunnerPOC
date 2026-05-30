using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.Mcp;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;
using ScriptRunner.GUI.Views;
using Splat;

namespace ScriptRunner.GUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            // Start MCP host if enabled
            var mcpHost = Locator.Current.GetService<ScriptRunnerMcpHost>();
            var settings = AppSettingsService.Load();
            if (settings.McpServer.Enabled && mcpHost != null)
            {
                _ = mcpHost.StartAsync(settings.McpServer);
            }

            // Stop MCP host on exit
            desktop.Exit += (_, _) =>
            {
                mcpHost?.StopAsync().GetAwaiter().GetResult();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
