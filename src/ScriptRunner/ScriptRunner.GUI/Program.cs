using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.Infrastructure.DataProtection;
using ScriptRunner.GUI.ViewModels;
using ScriptRunner.GUI.Views;
using IDataProtector = ScriptRunner.GUI.Infrastructure.DataProtection.IDataProtector;

namespace ScriptRunner.GUI;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseReactiveUI()
        .UsePlatformDetect()
        .WithIcons(container => container
            .Register<FontAwesomeIconProvider>())
        .UseMicrosoftDependencyInjection(ConfigureServices)
        .LogToTrace();

    private static void ConfigureServices(IServiceCollection services, IRuntimePlatform runtimePlatform)
    {
        var runtimeInfo = runtimePlatform.GetRuntimeInfo();
        if (runtimeInfo.OperatingSystem == OperatingSystemType.WinNT)
        {
            services.AddSingleton<IDataProtector, WindowsDataProtector>();
        }
        else if (runtimeInfo.OperatingSystem == OperatingSystemType.OSX)
        {
            services.AddDataProtectionConfigured();
            services.AddSingleton<IDataProtector, MacDataProtector>();
        }
        else
        {
            services.AddSingleton<IDataProtector, NullDataProtector>();
        }

        services.AddSingleton<VaultProvider>();
        services.AddSingleton<ParamsPanelFactory>();

        services.AddTransient<VaultViewModel>();
        services.AddTransient<VaultPickerViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }
}