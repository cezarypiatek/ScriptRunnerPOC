using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Threading;
using DynamicData;
using ReactiveUI;
using ScriptRunner.GUI.BackgroundTasks;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.Infrastructure.DataProtection;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ScriptReader;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI.ViewModels;

public class MainWindowViewModel : ReactiveObject
{


    private readonly ParamsPanelFactory _paramsPanelFactory;
    private readonly VaultProvider _vaultProvider;

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
    public List<ScriptConfig> Actions
    {
        get => _actions;
        set => this.RaiseAndSetIfChanged(ref _actions, value);
    }


    public string ActionFilter
    {
        get => _actionFilter;
        set => this.RaiseAndSetIfChanged(ref _actionFilter, value);
    }

    private string _actionFilter;

    private readonly ObservableAsPropertyHelper<IEnumerable<ScriptConfigGroupWrapper>> _filteredActionList;
    public IEnumerable<ScriptConfigGroupWrapper> FilteredActionList => _filteredActionList.Value;


    public ObservableCollection<RunningJobViewModel> RunningJobs { get; set; } = new();

    public RunningJobViewModel SelectedRunningJob
    {
        get => _selectedRunningJob;
        set => this.RaiseAndSetIfChanged(ref _selectedRunningJob, value);
    }

    public bool IsActionSelected
    {
        get => _isActionSelected;
        set => this.RaiseAndSetIfChanged(ref _isActionSelected, value);
    }

    private bool _isActionSelected;




    public ScriptConfig? SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (value == null && _selectedAction != null)
            {
                var prev = _selectedAction;
                Dispatcher.UIThread.Post(() =>
                {
                    if (Actions.FirstOrDefault(x => x.Name == prev.Name) is { } old)
                    {
                        SelectedAction = old;
                    }
                });
            }

            this.RaiseAndSetIfChanged(ref _selectedAction, value);
            if (value != null)
            {
                SelectedArgumentSet = value.PredefinedArgumentSets.FirstOrDefault();
                SelectedActionInstalled = string.IsNullOrWhiteSpace(value.InstallCommand) ? true : IsActionInstalled(value.Name);
                IsActionSelected = true;
            }
            else
            {
                SelectedArgumentSet = null;
                SelectedActionInstalled = false;
                IsActionSelected = false;
            }
        }
    }

    private bool IsActionInstalled(string valueName)
    {
        if (AppSettingsService.Load().InstalledActions is { } installedActions && installedActions.TryGetValue(valueName, out var installInfo))
        {
            return installInfo.IsInstalled;
        }
        return false;
    }

    public bool IsNewerVersionAvailable
    {
        get => _isNewerVersionAvailable;
        set => this.RaiseAndSetIfChanged(ref _isNewerVersionAvailable, value);
    }

    private bool _isNewerVersionAvailable;


    public ObservableCollection<OutdatedRepositoryModel> OutOfDateConfigRepositories { get; } = new();



    public MainWindowViewModel() : this(new ParamsPanelFactory(new VaultProvider(new NullDataProtector())), new VaultProvider(new NullDataProtector()))
    {
    }


    public MainWindowViewModel(ParamsPanelFactory paramsPanelFactory, VaultProvider vaultProvider)
    {
        _paramsPanelFactory = paramsPanelFactory;
        _vaultProvider = vaultProvider;
        this.appUpdater = new GithubUpdater();

        this.WhenAnyValue(x => x.ActionFilter, x => x.Actions)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .DistinctUntilChanged()
            .Select((pair, cancellationToken) =>
            {
                
                var configs = string.IsNullOrWhiteSpace(pair.Item1)? pair.Item2:pair.Item2.Where(x => x.Name.Contains(pair.Item1, StringComparison.InvariantCultureIgnoreCase));


                IEnumerable<ScriptConfigGroupWrapper> scriptConfigGroupWrappers = configs.SelectMany(c =>
                    {
                        if (c.Categories is {Count: > 0})
                        {
                            return c.Categories.Select((cat) => (category: cat, script: c));
                        }

                        return new[] {(category: "(No Category)", script: c)};
                    }).GroupBy(x => x.category).OrderBy(x=>x.Key)
                    .Select(x=> new ScriptConfigGroupWrapper
                    {
                        Name = x.Key,
                        Children = x.Select(p=>p.script).OrderBy(x=>x.Name)
                    });
                return scriptConfigGroupWrappers;
                
                
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.FilteredActionList, out _filteredActionList);

        _appUpdateScheduler = new RealTimeScheduler(TimeSpan.FromDays(1), TimeSpan.FromHours(1), async () =>
        {
            var isNewerVersion = await appUpdater.CheckIsNewerVersionAvailable();
            if (isNewerVersion)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsNewerVersionAvailable = true;
                });
            }
        });
        _appUpdateScheduler.Run();

        _outdatedRepoCheckingScheduler = new RealTimeScheduler(TimeSpan.FromHours(
            4), TimeSpan.FromHours(1), async () =>
        {
            var outOfDateRepos = await ConfigRepositoryUpdater.CheckAllRepositories();
            Dispatcher.UIThread.Post(() =>
            {
                OutOfDateConfigRepositories.Clear();
                OutOfDateConfigRepositories.AddRange(outOfDateRepos);
            });
        });

        _outdatedRepoCheckingScheduler.Run();


        ActionParametersPanel = new ObservableCollection<IPanel>();
        ParameterSetsForCurrentAction = new ObservableCollection<ArgumentSet>();
        BuildUi();
    }

    public void CheckForUpdates()
    {
        appUpdater.OpenLatestReleaseLog();
        
    }

    public void InstallUpdate()
    {
        appUpdater.InstallLatestVersion();
    }


    private IEnumerable<IControlRecord> _controlRecords;

    //private ActionsConfig config;
    
    private ScriptConfig _selectedAction;
    private ArgumentSet _selectedArgumentSet;

    private void BuildUi()
    {
        var selectedActionName = SelectedAction?.Name;
        var sources = AppSettingsService.Load().ConfigScripts ?? new List<ConfigScriptEntry>
        {
            new ConfigScriptEntry
            {
                Name = "Samples",
                Path = Path.Combine(AppContext.BaseDirectory,"Scripts/TextInputScript.json"),
                Type = ConfigScriptType.File
            }
        };
        var actions = new List<ScriptConfig>();
        foreach (var action in  sources.SelectMany(x=> ScriptConfigReader.Load(x)).OrderBy(x=>x.SourceName).ThenBy(x=>x.Name))
        {
            actions.Add(action);
        }

        Actions = actions;

        if (string.IsNullOrWhiteSpace(selectedActionName) == false && Actions.FirstOrDefault(x => x.Name == selectedActionName) is { } previouslySelected)
        {
            SelectedAction = previouslySelected;
        }
        else if(Actions.FirstOrDefault() is { } firstAction)
        {
            SelectedAction = firstAction;
        }
    }

    public ArgumentSet? SelectedArgumentSet
    {
        get => _selectedArgumentSet;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedArgumentSet, value);
            if (SelectedAction is { } selectedAction && _selectedArgumentSet is { Arguments: {}  arguments, Description: var setName})
            {
                if (setName == DefaultParameterSetName)
                {
                    if (AppSettingsService.TryGetDefaultOverrides(selectedAction.Name) is { } overrides)
                    {
                        arguments = new Dictionary<string, string>(arguments);
                        foreach (var (argName, argValue) in overrides)
                        {
                            arguments[argName] = argValue;
                        }
                    }
                }

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
        var paramsPanel = _paramsPanelFactory.Create(action, parameterValues);

        // Add panel with param controls to action panel
        //actionPanel.Children.Add(paramsPanel.Panel);

        // Add action panel to root container with all action panels
        ActionParametersPanel.Add(paramsPanel.Panel);

        // Write down param controls to read easier later - TODO: figure out better way, support multiple actions
        _controlRecords = paramsPanel.ControlRecords;
    }

    private int jobCounter;
    private RunningJobViewModel _selectedRunningJob;
    private bool _selectedActionInstalled;
    private readonly GithubUpdater appUpdater;
    private readonly RealTimeScheduler _appUpdateScheduler;
    private readonly RealTimeScheduler _outdatedRepoCheckingScheduler;
    private List<ScriptConfig> _actions = new ();

    public void InstallScript()
    {
        if (SelectedAction is { InstallCommand: {} installCommand } selectedAction )
        {
            var (commandPath, args) = SplitCommandAndArgs(installCommand);
            var job = new RunningJobViewModel
            {
                Tile = "#" + jobCounter++,
                CommandName = $"Install {selectedAction.Name}",
                ExecutedCommand = installCommand,
                EnvironmentVariables = new Dictionary<string, string?>()
            };
            job.ExecutionCompleted += (sender, eventArgs) =>
            {
                SelectedActionInstalled = true;
                AppSettingsService.MarkActionAsInstalled(selectedAction.Name);
            };
            this.RunningJobs.Add(job);
            SelectedRunningJob = job;
            job.RunJob(commandPath, args, selectedAction.InstallCommandWorkingDirectory, new ());
        }
    }

    public void OpenSettingsWindow()
    {
        var window = new SettingsWindow();
        window.Closed += (sender, args) =>
        {
            RefreshSettings();
        };
        window.Show();
    }

    public void RefreshSettings()
    {
        BuildUi();
    }

    public void OpenVaultWindow()
    {
        var window = new Vault();
        window.Show();
    }

    public async void PullRepoChanges(object arg)
    {
        if (arg is OutdatedRepositoryModel record)
        {
            var result = false;
            await Task.Run(async () => result = await ConfigRepositoryUpdater.PullRepository(record.Path));
            if (result)
            {
                OutOfDateConfigRepositories.Remove(record);
                RefreshSettings();
            }
        }
    }

    public bool SelectedActionInstalled
    {
        get => _selectedActionInstalled;
        set => this.RaiseAndSetIfChanged(ref _selectedActionInstalled, value);
    }

    private static (string commandPath, string args) SplitCommandAndArgs(string command)
    {
        var parts = SplitCommand(command);
        return (parts.Length > 0 ? parts[0] : "", parts.Length > 1 ? parts[1] : "");
    }

    public const string VaultReferencePrefix = "!!vault:";
    public const string DefaultParameterSetName = "<default>";

    public void SaveAsDefault()
    {
        if (SelectedAction == null)
        {
            return;
        }

        var defaultOverrides = new Dictionary<string, string>();
        foreach (var controlRecord in _controlRecords)
        {
            if (controlRecord is PasswordControl {Control: PasswordBox passwordBox})
            {
                if (string.IsNullOrWhiteSpace(passwordBox.VaultKey) == false)
                {
                    defaultOverrides[controlRecord.Name] = $"{VaultReferencePrefix}{passwordBox.VaultKey}";
                }
                else if (string.IsNullOrWhiteSpace(passwordBox.Password) == false)
                {
                    var vaultEntries = _vaultProvider.ReadFromVault().ToList();
                    var vaultKeyName = $"!default_{SelectedAction.Name}_{controlRecord.Name}";
                    var existingEntry = vaultEntries.FirstOrDefault(x => x.Name == vaultKeyName);
                    if (existingEntry != null)
                    {
                        vaultEntries.Remove(existingEntry);
                    }
                    vaultEntries.Add(new VaultEntry
                    {
                        Name = vaultKeyName,
                        Secret = passwordBox.Password
                    });
                    _vaultProvider.UpdateVault(vaultEntries);
                    defaultOverrides[controlRecord.Name] = $"{VaultReferencePrefix}{vaultKeyName}";
                }
            }
            else
            {
                var controlValue = controlRecord.GetFormattedValue();
                defaultOverrides[controlRecord.Name] = controlValue;
            }
        }
        AppSettingsService.UpdateDefaultOverrides(new ActionDefaultOverrides()
        {
            ActionName = SelectedAction.Name,
            Defaults = defaultOverrides
        });
    }


    public void RunScript()
    {
        if (SelectedAction is { } selectedAction)
        {
            
            var (commandPath, args) = SplitCommandAndArgs(selectedAction.Command);
            var maskedArgs = args;

            var envVariables = new Dictionary<string, string?>(selectedAction.EnvironmentVariables);

            foreach (var controlRecord in _controlRecords)
            {
                // This is definitely not pretty, should be using some ReactiveUI observables to read values?
                var controlValue = controlRecord.GetFormattedValue();
                args = args.Replace($"{{{controlRecord.Name}}}", controlValue);
                maskedArgs = maskedArgs.Replace($"{{{controlRecord.Name}}}", controlRecord.MaskingRequired? "*****": controlValue);

                foreach (var (key, val) in envVariables)
                {
                    if(string.IsNullOrWhiteSpace(val) == false)
                        envVariables[key] = val.Replace($"{{{controlRecord.Name}}}", controlValue);
                }
            }

            var job = new RunningJobViewModel
            {
                Tile = "#"+jobCounter++,
                CommandName = selectedAction.Name,
                ExecutedCommand = $"{commandPath} {maskedArgs}",
                EnvironmentVariables = envVariables
            };
            this.RunningJobs.Add(job);
            SelectedRunningJob = job;
            job.RunJob(commandPath, args, selectedAction.WorkingDirectory, selectedAction.InteractiveInputs);
        }
        
    }

    public void CloseJob(object arg)
    {
        if (arg is RunningJobViewModel job)
        {
            job.CancelExecution();
            RunningJobs.Remove(job);
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

public class ScriptConfigGroupWrapper
{
    public string Name { get; set; }
    public IEnumerable<ScriptConfig> Children { get; set; }
}
