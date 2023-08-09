using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ScriptRunner.GUI.ViewModels;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace ScriptRunner.GUI;

public class ViewLocator : IDataTemplate
{
    private readonly IServiceProvider _serviceProvider;

    public ViewLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public Control Build(object data)
    {
        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)ActivatorUtilities.CreateInstance(_serviceProvider, type);
        }
        else
        {
            return new TextBlock { Text = "Not Found: " + name };
        }
    }

    public bool Match(object data)
    {
        return data is ViewModelBase;
    }
}