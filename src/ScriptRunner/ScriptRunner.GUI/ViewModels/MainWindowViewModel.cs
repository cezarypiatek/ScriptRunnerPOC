using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CliWrap;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
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

    public string SelectedActionName
    {
        get => _selectedActionName;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedActionName, value);
            if (TryGetSelectedAction() is { } selectedAction)
            {
                RenderParameterForm(selectedAction);
            }
        }
    }

    public MainWindowViewModel()
    {
        ActionPanelsCollection = new ObservableCollection<IPanel>();
        Actions = new ObservableCollection<string>();

        BuildUi();
    }

    private IEnumerable<IControlRecord> _controlRecords;

    private ActionsConfig config;
    private string _currentRunOutput;
    private int _outputIndex;
    private string _selectedActionName;

    private void BuildUi()
    {
        config = ScriptConfigReader.Load();
        foreach (var action in config.Actions)
        {
            Actions.Add(action.Name);
        }

        if (Actions.FirstOrDefault() is { } actionToSelect)
        {
            SelectedActionName = actionToSelect;
        }
    }

    private void RenderParameterForm(ScriptConfig action)
    {
        ActionPanelsCollection.Clear();
        CurrentRunOutput = string.Empty;

        // Action panel could be used by creating custom user control with Name, description etc.
        // Just ParamsPanel should be generated dynamically
        var actionPanel = new StackPanel();
        actionPanel.Children.AddRange(new List<IControl>
        {
            new Label {Content = action.Name},
            new TextBlock {Text = action.Description},
            new TextBlock {Text = action.Command},
            new Label {Content = "Parameters: "}
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

    public void RunScript()
    {
        var parameters = new Dictionary<string, object>();
        foreach (var controlRecord in _controlRecords)
        {
            // This is definitely not pretty, should be using some ReactiveUI observables to read values?
            var controlValue = controlRecord.GetValue();
            var convertedValue = Convert.ChangeType(controlValue, controlRecord.ValueType);
            parameters[controlRecord.Name] = convertedValue;
        }

        if (TryGetSelectedAction() is { } selectedAction)
        {
            //TODO: handle whitespaces in the path
            var parts = selectedAction.Command.Split(' ', 2);
            var (commandPath, args) = (parts.Length > 0 ? parts[0] : "", parts.Length > 1 ? parts[1] : "");

            foreach (var parameter in parameters)
            {
                args = args.Replace($"{{{parameter.Key}}}", parameter.Value.ToString());
            }

            CurrentRunOutput = "";
            Task.Run(async () =>
            {
                try
                {
                    AppendToOutput("Execute the command:");
                    AppendToOutput($"{commandPath} {args}");
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    await Cli.Wrap(commandPath)
                        .WithArguments(args)
                        //TODO: Working dir should be read from the config with the fallback set to the config file dir
                        .WithWorkingDirectory(selectedAction.WorkingDirectory ?? "Scripts/")
                        .WithStandardOutputPipe(PipeTarget.ToDelegate(AppendToOutput))
                        .WithStandardErrorPipe(PipeTarget.ToDelegate(AppendToOutput))
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteAsync();
                    stopWatch.Stop();
                    AppendToOutput($"Execution finished after {stopWatch.Elapsed}");
                }
                catch (Exception e)
                {
                    AppendToOutput(e.Message);
                    AppendToOutput(e.StackTrace);
                }
            });
        }
        
    }

    private ScriptConfig? TryGetSelectedAction()
    {
        return config.Actions.FirstOrDefault(x => x.Name == SelectedActionName);
    }

    private void AppendToOutput(string? s)
    {
        if (s!=null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                CurrentRunOutput += s + Environment.NewLine;
                OutputIndex = CurrentRunOutput.Length;
            });
        }
    }

    public string CurrentRunOutput
    {
        get => _currentRunOutput;
        set => this.RaiseAndSetIfChanged(ref _currentRunOutput, value);
    }

    public int OutputIndex
    {
        get => _outputIndex;
        set => this.RaiseAndSetIfChanged(ref _outputIndex, value);
    }
}