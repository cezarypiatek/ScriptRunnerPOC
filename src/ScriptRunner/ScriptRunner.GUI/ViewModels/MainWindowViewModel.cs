using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CliWrap;
using DynamicData;
using MsBox.Avalonia;
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

    public bool IsScriptListVisible
    {
        get => _isScriptListVisible;
        set => this.RaiseAndSetIfChanged(ref _isScriptListVisible, value);
    }

    private bool _isScriptListVisible;


    public bool IsRecentListVisible
    {
        get => _isRecentListVisible;
        set => this.RaiseAndSetIfChanged(ref _isRecentListVisible, value);
    }

    private bool _isRecentListVisible;


    public bool IsSideBoxVisible => _isSideBoxVisible.Value;


    private readonly ObservableAsPropertyHelper<bool> _isSideBoxVisible; 


    

    public IReactiveCommand SaveAsPredefinedCommand { get; set; }


    private readonly ParamsPanelFactory _paramsPanelFactory;
    private readonly VaultProvider _vaultProvider;

    /// <summary>
    /// Contains panels with generated controls for every defined action
    /// </summary>
    private ObservableCollection<Panel> _actionParametersPanel;
    public ObservableCollection<Panel> ActionParametersPanel
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
  
    private bool _isActionSelected;
    public bool IsActionSelected
    {
        get => _isActionSelected;
        set => this.RaiseAndSetIfChanged(ref _isActionSelected, value);
    }

    private bool _installAvailable;
    public bool InstallAvailable
    {
        get => _installAvailable;
        set => this.RaiseAndSetIfChanged(ref _installAvailable, value);
    }

    private bool _hasParams;
    public bool HasParams
    {
        get => _hasParams;
        private set => this.RaiseAndSetIfChanged(ref _hasParams, value);
    }

    private object? _selectedActionOrGroup;
    
    public object? SelectedActionOrGroup
    {
        get => _selectedActionOrGroup;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedActionOrGroup, value);
            if (value is TaggedScriptConfig {Config: var scriptConfig})
            {
                SelectedAction = scriptConfig;
            }
        }
    }

    public ScriptConfig? SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (value == null || _selectedAction == value)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedAction, value);
            SelectedArgumentSet = value.PredefinedArgumentSets.FirstOrDefault();
            InstallAvailable = string.IsNullOrWhiteSpace(value.InstallCommand) == false;
            SelectedActionInstalled = InstallAvailable == false || IsActionInstalled(value.Name);
            IsActionSelected = true;
            HasParams = value.Params.Any();
        }
    }
    
    public KeyGesture SearchBoxHotKey
    {
        get
        {
            if (OperatingSystem.IsMacOS())
            {
                // Cmd+P
                return new KeyGesture(Key.P, KeyModifiers.Meta);
            }
            // Ctrl+P
            return new KeyGesture(Key.P, KeyModifiers.Control);
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

    public bool ShowNewVersionAvailable
    {
        get => _showNewVersionAvailable;
        set => this.RaiseAndSetIfChanged(ref _showNewVersionAvailable, value);
    }

    private bool _showNewVersionAvailable;


    public ObservableCollection<OutdatedRepositoryModel> OutOfDateConfigRepositories { get; } = new();



    public MainWindowViewModel() : this(new ParamsPanelFactory(new VaultProvider(new NullDataProtector())), new VaultProvider(new NullDataProtector()))
    {
    }


    public MainWindowViewModel(ParamsPanelFactory paramsPanelFactory, VaultProvider vaultProvider)
    {
        this._configRepositoryUpdater = new ConfigRepositoryUpdater(new CliRepositoryClient(command =>
        {
            var tcs = new TaskCompletionSource<CliCommandOutputs>();
            
            var job = new RunningJobViewModel
            {
                Tile = $"Update repository",
                ExecutedCommand = $"{command.Command} {command.Parameters}",
            };
            this.RunningJobs.Add(job);
            SelectedRunningJob = job;

            job.ExecutionCompleted += (sender, args) =>
            {
                tcs.SetResult(new(job.RawOutput, job.RawErrorOutput));
            };        
            job.RunJob(command.Command, command.Parameters, command.WorkingDirectory, Array.Empty<InteractiveInputDescription>(), Array.Empty<TroubleshootingItem>());
            return tcs.Task;
        }));
        IsScriptListVisible = true;
        SaveAsPredefinedCommand = ReactiveCommand.Create(() => { });
        _paramsPanelFactory = paramsPanelFactory;
        _vaultProvider = vaultProvider;
        this.appUpdater = new GithubUpdater();

        this.WhenAnyValue(x=>x.SelectedRecentExecution)
            .Where(x=>x is not null)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(b =>
            {
                if (Actions.FirstOrDefault(x => x.Name == b.Name && x.SourceName == b.Source) is { } selected)
                {
                    SelectedAction = selected;
                    RenderParameterForm(selected, b.Parameters);
                }
            });
        
        this.WhenAnyValue(x => x.IsScriptListVisible, x => x.IsRecentListVisible)
            .Select((t1, t2) => (t1.Item1 || t1.Item2))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.IsSideBoxVisible, out _isSideBoxVisible);
        
        this.WhenAnyValue(x => x.IsScriptListVisible)
            .Where(x => x)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(b => IsRecentListVisible = false);
        
        this.WhenAnyValue(x => x.IsRecentListVisible)
            .Where(x => x)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(b => IsScriptListVisible = false);
        
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
                            return c.Categories.DistinctBy(x=>x).Select((cat) => (category: cat, script: c));
                        }

                        return new[] {(category: "(No Category)", script: c)};
                    }).GroupBy(x => x.category).OrderBy(x=>x.Key)
                    .Select(x=> new ScriptConfigGroupWrapper
                    {
                        Name = x.Key,
                        Children = x.Select(p=> new TaggedScriptConfig(x.Key, p.script.Name, p.script)).OrderBy(x=>x.Name)
                    });
                return scriptConfigGroupWrappers;
                
                
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.FilteredActionList, out _filteredActionList);
            
        Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => this.ExecutionLog.CollectionChanged += h,
                h => this.ExecutionLog.CollectionChanged -= h)
            .Select(_ => Unit.Default) // We don't care about the event args; we just want to know something changed.
            .StartWith(Unit.Default) // To ensure initial population.
            .CombineLatest(this.WhenAnyValue(x => x.SelectedAction).Where(x => x != null),
                (_, selectedAction) => selectedAction)
            .Select(selectedAction =>
            {
                return this.ExecutionLog
                    .Where(y => y.Source == selectedAction.SourceName && y.Name == selectedAction.Name);
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.ExecutionLogForCurrent, out _executionLogForCurrent);
            
        
        _appUpdateScheduler = new RealTimeScheduler(TimeSpan.FromDays(1), TimeSpan.FromHours(1), async () =>
        {
            await RefreshInfoAbouAppUpdates();
        });
        _appUpdateScheduler.Run();

        _outdatedRepoCheckingScheduler = new RealTimeScheduler(TimeSpan.FromHours(4), TimeSpan.FromHours(1), async () =>
        {
            await RefreshInfoAboutRepositories();
        });

        _outdatedRepoCheckingScheduler.Run();


        ActionParametersPanel = new ObservableCollection<Panel>();
        BuildUi();
    }

    private async Task RefreshInfoAbouAppUpdates()
    {
        var isNewerVersion = await appUpdater.CheckIsNewerVersionAvailable();
        if (isNewerVersion)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ShowNewVersionAvailable = true;
            });
        }
    }

    private async Task RefreshInfoAboutRepositories()
    {
        var outOfDateRepos = await _configRepositoryUpdater.CheckAllRepositories();
        Dispatcher.UIThread.Post(() =>
        {
            OutOfDateConfigRepositories.Clear();
            OutOfDateConfigRepositories.AddRange(outOfDateRepos);
        });
    }

    public void CheckForUpdates()
    {
        appUpdater.OpenLatestReleaseLog();
        
    }

    public void InstallUpdate()
    {
        appUpdater.InstallLatestVersion();
    }

    public void DismissNewVersionAvailable()
    {
        ShowNewVersionAvailable = false;
    }

    private IEnumerable<IControlRecord> _controlRecords;

    //private ActionsConfig config;
    
    private ScriptConfig _selectedAction;
    private ArgumentSet _selectedArgumentSet;

    private void BuildUi()
    {
        var selectedActionName = SelectedAction?.Name;
        var appSettings = AppSettingsService.Load();
        var sources = appSettings.ConfigScripts == null || appSettings.ConfigScripts.Count == 0
            ? SampleScripts
            : appSettings.ConfigScripts;
        var actions = new List<ScriptConfig>();
        foreach (var action in  sources.SelectMany(x=> ScriptConfigReader.Load(x, appSettings)).OrderBy(x=>x.SourceName).ThenBy(x=>x.Name))
        {
            actions.Add(action);
        }

        Actions = actions;

        if (string.IsNullOrWhiteSpace(selectedActionName) == false && Actions.FirstOrDefault(x => x.Name == selectedActionName) is { } previouslySelected)
        {
            SelectedAction = previouslySelected;
        }
        else if (appSettings.Recent?.OrderByDescending(x => x.Value.Timestamp).FirstOrDefault() is { } recent && Actions.FirstOrDefault(a =>
                         a.Name == recent.Value?.ActionId.ActionName &&
                         a.SourceName == recent.Value.ActionId.SourceName) is
                     { } existingRecent)
        {
            SelectedAction = existingRecent;
            if (existingRecent.PredefinedArgumentSets.FirstOrDefault(p =>
                    p.Description == recent.Value.ActionId.ParameterSet) is { } ps)
            {
                SelectedArgumentSet = ps;
            }
        }
        else if(Actions.FirstOrDefault() is { } firstAction)
        {
            SelectedAction = firstAction;
        }
        ExecutionLog.Clear();
        ExecutionLog.AddRange(AppSettingsService.LoadExecutionLog());
    }

    private static List<ConfigScriptEntry> SampleScripts => new()
    {
        new ConfigScriptEntry
        {
            Name = "Samples",
            Path = Path.Combine(AppContext.BaseDirectory,"Scripts/TextInputScript.json"),
            Type = ConfigScriptType.File
        }
    };

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


    private void RenderParameterForm(ScriptConfig action, Dictionary<string, string> parameterValues)
    {
        ActionParametersPanel.Clear();


        // Action panel could be used by creating custom user control with Description, description etc.
        // Just ParamsPanel should be generated dynamically
        //var actionPanel = new StackPanel();

        // Create IPanel with controls for all parameters
        var paramsPanel = _paramsPanelFactory.Create(action, parameterValues,  (title, command) =>
        {
            if (SelectedAction != null)
            {
                var taskCompletionSource = new TaskCompletionSource<string>();
                try
                {
                    ExecuteCommand(command, this.SelectedAction, title, s =>
                    {
                        taskCompletionSource.SetResult(s);
                    });
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
                return taskCompletionSource.Task;
            }

            return Task.FromResult("");
        });

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

    public void ResetDefaults()
    {
        if (SelectedAction is not null)
        {
            AppSettingsService.UpdateDefaultOverrides(new ActionDefaultOverrides
            {
                ActionName = SelectedAction.Name,
                Defaults = new Dictionary<string, string>()
            });
            BuildUi();
        }
    }
    public void InstallScript()
    {
        if (SelectedAction is { InstallCommand: {} installCommand } selectedAction )
        {
            if (selectedAction.RunInstallCommandAsAdmin && IsAdministrator() == false)
            {
                NotifyAboutMissingAdminRights();
                return;
            }

            var (commandPath, args) = SplitCommandAndArgs(installCommand);
            var job = new RunningJobViewModel
            {
                Tile = $"#{jobCounter++} Install {selectedAction.Name}",
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
            job.RunJob(commandPath, args, selectedAction.InstallCommandWorkingDirectory, Array.Empty<InteractiveInputDescription>(), selectedAction.InstallTroubleshooting);
        }
    }

    private static void NotifyAboutMissingAdminRights()
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard("Missing permissions", "This scripts requires administrator rights.\r\n\r\nPlease restart the app as administrator. ", icon: MsBox.Avalonia.Enums.Icon.Forbidden);
        messageBoxStandardWindow.ShowAsync();
    }

    public static bool IsAdministrator()
    {
        if (OperatingSystem.IsWindows())
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        return true;
    }

    public void OpenSettingsWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime {MainWindow: {} mainWindow})
        {
            var window = new SettingsWindow();
            window.Closed += (sender, args) =>
            {
                RefreshSettings();
            };
            window.Show(mainWindow);
        }
    }

    public void ForceRefresh()
    {
        _ = RefreshInfoAbouAppUpdates();
        _ = RefreshInfoAboutRepositories();
        BuildUi();
    }
    public void RefreshSettings() => BuildUi();

    public void OpenVaultWindow() => TryToOpenDialog<Vault>();

    public void TryToOpenDialog<T>(Action? callback = null) where T : Window, new()
    {
        var window = new T();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime {MainWindow: {} mainWindow})
        {
            window.ShowDialog(mainWindow);
            callback?.Invoke();
        }
    }

    public async void PullRepoChanges(object arg)
    {
        if (arg is OutdatedRepositoryModel record)
        {
            record.IsPulling = true;
            var result = false;
            try
            {
                await Task.Run(async () => result = await _configRepositoryUpdater.RefreshRepository(record.Path));
            }
            finally
            {
                if (result)
                {
                    OutOfDateConfigRepositories.Remove(record);
                    RefreshSettings();
                }
                record.IsPulling = false;
            }
            
        }
    }

    public bool SelectedActionInstalled
    {
        get => _selectedActionInstalled;
        set => this.RaiseAndSetIfChanged(ref _selectedActionInstalled, value);
    }

    public static (string commandPath, string args) SplitCommandAndArgs(string command)
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

        var defaultOverrides = HarvestCurrentParameters(vaultPrefixForNewEntries: $"{SelectedAction.Name}_{DefaultParameterSetName}");
        AppSettingsService.UpdateDefaultOverrides(new ActionDefaultOverrides
        {
            ActionName = SelectedAction.Name,
            Defaults = defaultOverrides
        });
        BuildUi();
    }

    public async void CopyParametersSetup()
    {
        var setup = HarvestCurrentParameters(string.Empty, includePasswords: false);
        var serialized = JsonSerializer.Serialize(setup, new JsonSerializerOptions()
        {
            WriteIndented = true
        });

        GetClipboard()!.SetTextAsync(serialized);

    }

    private IClipboard? GetClipboard()
    {
        if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow.Clipboard;
        }

        return null;
    }
    
    public async void PasteParametersSetup()
    {
        var payload = await GetClipboard()!.GetTextAsync();
        if (payload.IndexOf('{') is { } first && payload.LastIndexOf('}') is { } last && first > -1 && last > -1 && last > first)
        {
            try
            {
                var actualPayload = payload.Substring(first, (last+1) - first);
                //INFO: When you send JSON via MSTeams, you will get extra non-breaking spaces on the other side. Those extra white-spaces breaks deserializer
                actualPayload = actualPayload.Replace(" "+ (char)160, " ");
                var data =  JsonSerializer.Deserialize<Dictionary<string, string>>(actualPayload);
                var currentSetup = HarvestCurrentParameters(string.Empty, includePasswords: true);

                foreach (var param in SelectedAction.Params)
                {
                    if (param.Prompt == PromptType.Password && currentSetup.TryGetValue(param.Name, out var password))
                    {
                        data[param.Name] = password;
                    }
                }

                RenderParameterForm(SelectedAction, data);
            }
            catch
            {
                var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard("Incorrect data", "Clipboard content is not valid JSON with parameter set", icon: MsBox.Avalonia.Enums.Icon.Error);
                await messageBoxStandardWindow.ShowAsync();
            }
        }
        else
        {
            var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard("Incorrect data", "Clipboard content is not JSON", icon:  MsBox.Avalonia.Enums.Icon.Error);
            await messageBoxStandardWindow.ShowAsync();
        }
    }

    private Dictionary<string, string> HarvestCurrentParameters(string vaultPrefixForNewEntries, bool includePasswords = true)
    {
        var defaultOverrides = new Dictionary<string, string>();
        foreach (var controlRecord in _controlRecords)
        {
            if (controlRecord is PasswordControl {Control: PasswordBox passwordBox})
            {
                if (includePasswords == false)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(passwordBox.VaultKey) == false)
                {
                    defaultOverrides[controlRecord.Name] = $"{VaultReferencePrefix}{passwordBox.VaultKey}";
                }
                else if (string.IsNullOrWhiteSpace(passwordBox.Password) == false)
                {
                    var vaultEntries = _vaultProvider.ReadFromVault().ToList();
                    var vaultKeyName = $"!{vaultPrefixForNewEntries}_{controlRecord.Name}";
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

        return defaultOverrides;
    }

    public void SaveAsPredefined(string setName)
    {
        if (SelectedAction == null)
        {
            return;
        }

        var defaultOverrides = HarvestCurrentParameters(vaultPrefixForNewEntries: $"{SelectedAction.Name}_{setName}");
        AppSettingsService.UpdateExtraParameterSet(new ActionExtraPredefinedParameterSet
        {
            ActionName = SelectedAction.Name,
            Description = setName,
            Arguments = defaultOverrides
        });

        var newSet = new ArgumentSet()
        {
            Arguments = defaultOverrides,
            Description = setName
        };
        var existing = SelectedAction.PredefinedArgumentSets.FirstOrDefault(x => x.Description == setName);
        if (existing != null)
        {

            SelectedAction.PredefinedArgumentSets.Replace(existing, newSet);
        }
        else
        {

            SelectedAction.PredefinedArgumentSets.Add(newSet);
            this.RaisePropertyChanged("SelectedAction.PredefinedArgumentSets");
            SelectedArgumentSet = newSet;
        }
        BuildUi();

    }

    public void OpenDirInVsCode()
    {
        if (SelectedAction is {} action)
        {
            var dirName = Path.GetDirectoryName(action.Source);
            Cli.Wrap("code").WithArguments(dirName).ExecuteAsync();
        }
    }
    
    public void OpenDefinitionInVsCode()
    {
        if (SelectedAction is {} action)
        {
            Cli.Wrap("code").WithArguments(action.Source).ExecuteAsync();
        }
    }


    public void RunScript()
    {
        if (SelectedAction is { } selectedAction)
        {
            if (selectedAction.RunCommandAsAdmin && IsAdministrator() == false)
            {
                NotifyAboutMissingAdminRights();
                return;
            }


            AddExecutionAudit(selectedAction);

            var selectedActionCommand = selectedAction.Command;
            
            ExecuteCommand(selectedActionCommand, selectedAction);

            // Some audit staff
            var usedParams = HarvestCurrentParameters(vaultPrefixForNewEntries: $"{selectedAction.Name}_{Guid.NewGuid():N}");
            var executionLogAction = new ExecutionLogAction(DateTime.Now,  selectedAction.SourceName, selectedAction.Name, usedParams);
            ExecutionLog.Insert(0, executionLogAction);
            SelectedRecentExecution = executionLogAction;
            AppSettingsService.UpdateExecutionLog(ExecutionLog.ToList());
        }
        
    }

    private void ExecuteCommand(string command, ScriptConfig selectedAction, string? title = null, Action<string>? onComplete = null)
    {
        var (commandPath, args) = SplitCommandAndArgs(command);
        var envVariables = new Dictionary<string, string?>(selectedAction.EnvironmentVariables);
        var maskedArgs = args;
        foreach (var controlRecord in _controlRecords)
        {
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
            Tile = $"#{jobCounter++} {title ?? selectedAction.Name}",
            ExecutedCommand = $"{commandPath} {maskedArgs}",
            EnvironmentVariables = envVariables
        };
        this.RunningJobs.Add(job);
        SelectedRunningJob = job;

        if (onComplete != null)
        {
            job.ExecutionCompleted += (sender, args) => onComplete(job.RawOutput);
        }
        
        job.RunJob(commandPath, args, selectedAction.WorkingDirectory, selectedAction.InteractiveInputs, selectedAction.Troubleshooting);
    }

    public ObservableCollection<ExecutionLogAction> ExecutionLog { get; set; } = new ();
    
    
    private readonly ObservableAsPropertyHelper<IEnumerable<ExecutionLogAction>>  _executionLogForCurrent;
    public IEnumerable<ExecutionLogAction>  ExecutionLogForCurrent => _executionLogForCurrent.Value;
    


    public ExecutionLogAction SelectedRecentExecution
    {
        get => _selectedRecentExecution;
        set => this.RaiseAndSetIfChanged(ref _selectedRecentExecution, value);
    }

    private ExecutionLogAction _selectedRecentExecution;
    private readonly ConfigRepositoryUpdater _configRepositoryUpdater;


    private void AddExecutionAudit(ScriptConfig selectedAction)
    {
        AppSettingsService.UpdateRecent(recent =>
        {
            var actionId = new ActionId(selectedAction.SourceName ?? string.Empty, selectedAction.Name, SelectedArgumentSet?.Description ?? "<default>");
            recent[$"{actionId.SourceName}__{actionId.ActionName}__{actionId.ParameterSet}"] = new RecentAction(actionId, DateTime.UtcNow);
        });
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

public record ExecutionLogAction(DateTime Timestamp, string Source, string Name, Dictionary<string, string> Parameters)
{
    [JsonIgnore]
    public string Description => $"{Timestamp:s} - [{string.Join(", ", Parameters.Select(x => $"{x.Key} = {x.Value}"))}]";
};

public record RecentAction(ActionId ActionId, DateTime Timestamp);

public record ActionId(string SourceName, string ActionName, string ParameterSet);

public class ScriptConfigGroupWrapper
{
    public string Name { get; set; }
    public IEnumerable<TaggedScriptConfig> Children { get; set; }
}

public record TaggedScriptConfig(string Tag, string Name, ScriptConfig Config);