using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using ReactiveUI;
using ScriptRunner.GUI.ScriptReader;

namespace ScriptRunner.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Contains panels with generated controls for every defined action
    /// </summary>
    private ObservableCollection<IPanel> _actionPanelsCollection;
    public ObservableCollection<IPanel> ActionPanelsCollection
    {
        get => _actionPanelsCollection;
        private set => this.RaiseAndSetIfChanged(ref _actionPanelsCollection, value);
    }

    /// <summary>
    /// Contains list of actions defined in json file
    /// </summary>
    private ObservableCollection<string> _actions;
    public ObservableCollection<string> Actions
    {
        get => _actions;
        private set => this.RaiseAndSetIfChanged(ref _actions, value);
    }

    public MainWindowViewModel()
    {
        ActionPanelsCollection = new ObservableCollection<IPanel>();
        Actions = new ObservableCollection<string>();

        BuildUi();
    }

    private IEnumerable<IControlRecord> _controlRecords;

    private void BuildUi()
    {
        var config = ScriptConfigReader.Load();
        foreach (var action in config.Actions)
        {
            Actions.Add(action.Name);
                
            // Action panel could be used by creating custom user control with Name, description etc.
            // Just ParamsPanel should be generated dynamically
            var actionPanel = new StackPanel();
            actionPanel.Children.AddRange(new List<IControl>
            {
                new Label { Content = action.Name },
                new TextBlock { Text = action.Description },
                new TextBlock { Text = action.Command },
                new Label { Content = "Parameters: " }
            });

            // Create IPanel with controls for all parameters
            var paramsPanel = new ParamsPanelFactory().Create(action.Params);

            // Add panel with param controls to action panel
            actionPanel.Children.Add(paramsPanel.Panel);

            // Add action panel to root container with all action panels
            ActionPanelsCollection.Add(actionPanel);

            // Write down param controls to read easier later - TODO: figure out better way, support multiple actions
            _controlRecords = paramsPanel.ControlRecords;
        }
    }

    public void RunScript()
    {
        foreach (var controlRecord in _controlRecords)
        {
            // This is definitely not pretty, should be using some ReactiveUI observables to read values?
            var controlValue = controlRecord.GetValue();
            var convertedValue = Convert.ChangeType(controlValue, controlRecord.ValueType);
        }
    }
}