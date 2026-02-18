using Avalonia;
using Avalonia.Labs.Notifications;
using Avalonia.ReactiveUI;
using System;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.Infrastructure.DataProtection;
using ScriptRunner.GUI.Services;
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
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();
        return AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .WithAppNotifications(new AppNotificationOptions
            {
                AppName = "ScriptRunner"
            })
            .UseMicrosoftDependencyInjection(ConfigureServices)
            .LogToTrace();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IDataProtector, WindowsDataProtector>();
        }
        else if (OperatingSystem.IsMacOS())
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
        services.AddSingleton<INotificationService, NotificationService>();

        services.AddTransient<VaultViewModel>();
        services.AddTransient<VaultPickerViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }
}