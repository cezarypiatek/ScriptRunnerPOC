using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace ScriptRunner.GUI.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static AppBuilder UseMicrosoftDependencyInjection(this AppBuilder builder, Action<IServiceCollection> configure)
    {
        var serviceCollection = new ServiceCollection();
        configure(serviceCollection);
        serviceCollection.UseMicrosoftDependencyResolver();
        return builder;
    }
}