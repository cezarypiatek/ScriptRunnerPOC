using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class ActionDetailsSection : UserControl
{
    public ActionDetailsSection()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async void SaveAsPredefined(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (viewModel?.SelectedAction == null)
            {
                return;
            }
            var popup = new PredefinedParameterSaveWindow();
            popup.DataContext = new SavePredefinedParameterVM()
            {
                UseNew = true,
                ExistingSets = viewModel.SelectedAction.PredefinedArgumentSets.Select(x => x.Description).ToList(),
                SelectedExisting = viewModel.SelectedArgumentSet?.Description
            };
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var sourceWindow = desktop.MainWindow;
                if (await popup.ShowDialog<string>(sourceWindow) is { } setName && string.IsNullOrWhiteSpace(setName) == false)
                {
                    if (setName == MainWindowViewModel.DefaultParameterSetName)
                    {
                       viewModel.SaveAsDefault();
                    }
                    else
                    {
                        viewModel.SaveAsPredefined(setName);
                    }
                }
            }
        }
    }
}