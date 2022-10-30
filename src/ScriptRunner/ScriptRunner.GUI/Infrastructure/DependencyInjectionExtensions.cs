using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace ScriptRunner.GUI.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static TAppBuilder UseMicrosoftDependencyInjection<TAppBuilder>(this TAppBuilder builder, Action<IServiceCollection, IRuntimePlatform> configure)
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        var serviceCollection = new ServiceCollection();
        configure(serviceCollection, builder.RuntimePlatform);
        serviceCollection.UseMicrosoftDependencyResolver();
        return builder;
    }
}