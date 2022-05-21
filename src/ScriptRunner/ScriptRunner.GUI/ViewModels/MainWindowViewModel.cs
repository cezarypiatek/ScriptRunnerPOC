using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ScriptReader;

namespace ScriptRunner.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Contains panels with generated controls for every defined action
    /// </summary>
    private ObservableCollection<IPanel> _actionParametersPanel;
    public ObservableCollection<IPanel> ActionParametersPanel
    {
        get => _actionParametersPanel;
        private set => this.RaiseAndSetIfChanged(ref _actionParametersPanel, value);
    }

    /// <summary>
    /// Contains list of actions defined in json file
    /// </summary>
    private ObservableCollection<ScriptConfig> _actions;
    public ObservableCollection<ScriptConfig> Actions
    {
        get => _actions;
        private set => this.RaiseAndSetIfChanged(ref _actions, value);
    }

    public ObservableCollection<RunningJobViewModel> RunningJobs { get; set; } = new();

    public RunningJobViewModel SelectedRunningJob
    {
        get => _selectedRunningJob;
        set => this.RaiseAndSetIfChanged(ref _selectedRunningJob, value);
    }

    public ScriptConfig SelectedAction
    {
        get => _selectedAction;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedAction, value);
            SelectedArgumentSet = value.PredefinedArgumentSets.First();
            
        }
    }

    public MainWindowViewModel()
    {
        ActionParametersPanel = new ObservableCollection<IPanel>();
        Actions = new ObservableCollection<ScriptConfig>();
        ParameterSetsForCurrentAction = new ObservableCollection<ArgumentSet>();
        BuildUi();
    }

    private IEnumerable<IControlRecord> _controlRecords;

    private ActionsConfig config;
    
    private ScriptConfig _selectedAction;
    private ArgumentSet _selectedArgumentSet;

    private void BuildUi()
    {
        config = ScriptConfigReader.Load();
        foreach (var action in config.Actions)
        {
            Actions.Add(action);
        }

        if (Actions.FirstOrDefault() is { } actionToSelect)
        {
            SelectedAction = actionToSelect;
        }
    }

    public ArgumentSet SelectedArgumentSet
    {
        get => _selectedArgumentSet;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedArgumentSet, value);
            if (SelectedAction is { } selectedAction && _selectedArgumentSet is { Arguments: {}  arguments})
            {
                RenderParameterForm(selectedAction, arguments);
            }
        }
    }

    public ObservableCollection<ArgumentSet> ParameterSetsForCurrentAction { get; set; }

    private void RenderParameterForm(ScriptConfig action, Dictionary<string, string> parameterValues)
    {
        ActionParametersPanel.Clear();


        // Action panel could be used by creating custom user control with Description, description etc.
        // Just ParamsPanel should be generated dynamically
        //var actionPanel = new StackPanel();

        // Create IPanel with controls for all parameters
        var paramsPanel = new ParamsPanelFactory().Create(action.Params, parameterValues);

        // Add panel with param controls to action panel
        //actionPanel.Children.Add(paramsPanel.Panel);

        // Add action panel to root container with all action panels
        ActionParametersPanel.Add(paramsPanel.Panel);

        // Write down param controls to read easier later - TODO: figure out better way, support multiple actions
        _controlRecords = paramsPanel.ControlRecords;
    }

    private int jobCounter;
    private RunningJobViewModel _selectedRunningJob;

    public void RunScript()
    {
        if (SelectedAction is { } selectedAction)
        {
            var command = selectedAction.Command;
            var parts = SplitCommand(command);
            var (commandPath, args) = (parts.Length > 0 ? parts[0] : "", parts.Length > 1 ? parts[1] : "");
            var maskedArgs = args;

            foreach (var controlRecord in _controlRecords)
            {
                // This is definitely not pretty, should be using some ReactiveUI observables to read values?
                var controlValue = controlRecord.GetFormattedValue();
                args = args.Replace($"{{{controlRecord.Name}}}", controlValue);
                maskedArgs = maskedArgs.Replace($"{{{controlRecord.Name}}}", controlRecord.MaskingRequired? "*****": controlValue);
            }

            var job = new RunningJobViewModel()
            {
                Tile = "#"+jobCounter++,
                CommandName = selectedAction.Name,
                ExecutedCommand = $"{commandPath} {maskedArgs}"
            };
            this.RunningJobs.Add(job);
            SelectedRunningJob = job;
            SelectedRunningJob = job;
            job.RunJob(commandPath, args, selectedAction);
        }
        
    }

    

    private static string[] SplitCommand(string command)
    {
        command = command.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            return new[] {string.Empty, string.Empty};
        }

        if (command.StartsWith('\''))
        {
            return command.TrimStart('\'').Split('\'', 2);
        }
        
        if (command.StartsWith('\"'))
        {
            return command.TrimStart('\"').Split('\"', 2);
        }

        return command.Split(' ', 2);
    }

    
}