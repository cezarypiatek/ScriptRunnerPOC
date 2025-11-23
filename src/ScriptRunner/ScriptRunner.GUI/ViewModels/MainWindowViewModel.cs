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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
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

    public bool IsStatisticsVisible
    {
        get => _isStatisticsVisible;
        set => this.RaiseAndSetIfChanged(ref _isStatisticsVisible, value);
    }

    private bool _isStatisticsVisible;

    public bool IsLoadingConfig
    {
        get => _isLoadingConfig;
        set
        {
            this.RaiseAndSetIfChanged(ref _isLoadingConfig, value);
            UpdateIsAnyRefreshInProgress();
        }
    }

    private bool _isLoadingConfig;

    public bool IsRefreshingAppUpdates
    {
        get => _isRefreshingAppUpdates;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRefreshingAppUpdates, value);
            UpdateIsAnyRefreshInProgress();
        }
    }

    private bool _isRefreshingAppUpdates;

    public bool IsRefreshingRepositories
    {
        get => _isRefreshingRepositories;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRefreshingRepositories, value);
            UpdateIsAnyRefreshInProgress();
        }
    }

    private bool _isRefreshingRepositories;

    public bool IsAnyRefreshInProgress
    {
        get => _isAnyRefreshInProgress;
        private set => this.RaiseAndSetIfChanged(ref _isAnyRefreshInProgress, value);
    }

    private bool _isAnyRefreshInProgress;

    private void UpdateIsAnyRefreshInProgress()
    {
        IsAnyRefreshInProgress = IsRefreshingAppUpdates || IsRefreshingRepositories || IsLoadingConfig;
    }

    public StatisticsViewModel Statistics { get; private set; }

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
        CompactedHistoryForCurrent = true;
        this._configRepositoryUpdater = new ConfigRepositoryUpdater(new CliRepositoryClient(command =>
        {
            var tcs = new TaskCompletionSource<CliCommandOutputs>();
            Dispatcher.UIThread.Post(() =>
            {
                var job = new RunningJobViewModel
                {
                    Tile = $"Update repository",
                    ExecutedCommand = $"{command.Command} {command.Parameters}",
                };
                job.ExecutedCommandFormatted.AddRange(CreateSimpleFormattedCommand($"{command.Command} {command.Parameters}"));
                this.RunningJobs.Add(job);
                SelectedRunningJob = job;

                job.ExecutionCompleted += (sender, args) =>
                {
                    tcs.SetResult(new(job.RawOutput, job.RawErrorOutput));
                };        
                job.RunJob(command.Command, command.Parameters, command.WorkingDirectory, Array.Empty<InteractiveInputDescription>(), Array.Empty<TroubleshootingItem>());
            }); 
            return tcs.Task;
        }));
        IsScriptListVisible = true;
        SaveAsPredefinedCommand = ReactiveCommand.Create(() => { });
        _paramsPanelFactory = paramsPanelFactory;
        _vaultProvider = vaultProvider;
        this.appUpdater = new GithubUpdater();

        // Initialize Statistics ViewModel
        Statistics = new StatisticsViewModel(ExecutionLog);

        ExecutionLogAction? lastSelected = null;
        
        this.WhenAnyValue(x=>x.SelectedRecentExecution)
            .Where(x=>x is not null && x != lastSelected)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(b =>
            {
                lastSelected = b;
                if (Actions.FirstOrDefault(x => x.Name == b.Name && x.SourceName == b.Source) is { } selected)
                {
                    
                    SelectedAction = selected;
                    RenderParameterForm(selected, b.Parameters);
                }
            });
        
        this.WhenAnyValue(x => x.IsScriptListVisible, x => x.IsRecentListVisible, x => x.IsStatisticsVisible)
            .Select(t => (t.Item1 || t.Item2 || t.Item3))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.IsSideBoxVisible, out _isSideBoxVisible);
        
        this.WhenAnyValue(x => x.IsScriptListVisible)
            .Where(x => x)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(b => { IsRecentListVisible = false; IsStatisticsVisible = false; });
        
        this.WhenAnyValue(x => x.IsRecentListVisible)
            .Where(x => x)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(b => { IsScriptListVisible = false; IsStatisticsVisible = false; });
        
        this.WhenAnyValue(x => x.IsStatisticsVisible)
            .Where(x => x)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(b => 
            { 
                IsScriptListVisible = false; 
                IsRecentListVisible = false;
                Statistics.RefreshStatistics();
            });
        
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
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(_ => Unit.Default) // We don't care about the event args; we just want to know something changed.
            .StartWith(Unit.Default) // To ensure initial population.
            .CombineLatest(
                this.WhenAnyValue(
                    x => x.SelectedAction, 
                    x=>x.CompactedHistoryForCurrent,
                    x=>x.TermForCurrentHistoryFilter
                    ).Where(x => x.Item1 != null).Throttle(TimeSpan.FromMilliseconds(200)),
                (_, selectedAction) => selectedAction)
            .Select(data =>
            {
                var (selectedAction, compacted, term) = data;
                var filtered = this.ExecutionLog.Where(y => y.Source == selectedAction!.SourceName && y.Name == selectedAction.Name);
                
                if (compacted)
                {
                    filtered = filtered.GroupBy(x => x.ParametersDescriptionString(), (key, group) => group.First());
                }

                if (string.IsNullOrWhiteSpace(term) == false)
                {
                    filtered = filtered.Where(x => x.Parameters.Values.Any(p => p?.Contains(term, StringComparison.InvariantCultureIgnoreCase) == true));
                }

                return filtered;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.ExecutionLogForCurrent, out _executionLogForCurrent);

        // Create grouped execution log with date dividers
        Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => this.ExecutionLog.CollectionChanged += h,
                h => this.ExecutionLog.CollectionChanged -= h)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .Select(_ =>
            {
                var items = new List<ExecutionLogItemBase>();
                DateTime? lastDate = null;
                
                foreach (var action in ExecutionLog)
                {
                    var actionDate = action.Timestamp.Date;
                    
                    // Add date header if the date changed
                    if (lastDate == null || lastDate != actionDate)
                    {
                        items.Add(new ExecutionLogDateHeader(actionDate));
                        lastDate = actionDate;
                    }
                    
                    items.Add(new ExecutionLogItemAction(action));
                }
                
                return items.AsEnumerable();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.ExecutionLogGrouped, out _executionLogGrouped);

        // Create available dates list for date picker
        Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => this.ExecutionLog.CollectionChanged += h,
                h => this.ExecutionLog.CollectionChanged -= h)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .Select(_ =>
            {
                // Group by date and count items
                var dateGroups = ExecutionLog
                    .GroupBy(x => x.Timestamp.Date)
                    .OrderByDescending(g => g.Key)
                    .Select(g => new DateGroupInfo(g.Key, g.Count()))
                    .ToList();
                
                return dateGroups.AsEnumerable();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.AvailableDates, out _availableDates);

        this.WhenAnyValue(x => x.SelectedAction)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(s =>
            {
                TermForCurrentHistoryFilter = "";
            });

        
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

    private bool _compactedHistoryForCurrent;

    public bool CompactedHistoryForCurrent
    {
        get => _compactedHistoryForCurrent;
        set => this.RaiseAndSetIfChanged(ref _compactedHistoryForCurrent, value);
    }

    private string _termForCurrentHistoryFilter;

    public string TermForCurrentHistoryFilter
    {
        get => _termForCurrentHistoryFilter;
        set => this.RaiseAndSetIfChanged(ref _termForCurrentHistoryFilter, value);
    }
    
    
    private async Task RefreshInfoAbouAppUpdates()
    {
        IsRefreshingAppUpdates = true;
        try
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
        finally
        {
            IsRefreshingAppUpdates = false;
        }
    }

    private async Task RefreshInfoAboutRepositories()
    {
        IsRefreshingRepositories = true;
        try
        {
            var outOfDateRepos = await _configRepositoryUpdater.CheckAllRepositories();
            Dispatcher.UIThread.Post(() =>
            {
                OutOfDateConfigRepositories.Clear();
                OutOfDateConfigRepositories.AddRange(outOfDateRepos);
            });
        }
        finally
        {
            IsRefreshingRepositories = false;
        }
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

    public void DismissOutdatedRepositories()
    {
        OutOfDateConfigRepositories.Clear();
    }
    private IEnumerable<IControlRecord> _controlRecords;

    //private ActionsConfig config;
    
    private ScriptConfig _selectedAction;
    private ArgumentSet _selectedArgumentSet;

    private void BuildUi()
    {
        IsLoadingConfig = true;
        Task.Run(() =>
        {
            var selectedActionName = SelectedAction?.Name;
            var appSettings = AppSettingsService.Load();
            var sources = appSettings.ConfigScripts == null || appSettings.ConfigScripts.Count == 0
                ? SampleScripts
                : appSettings.ConfigScripts;
            var actions = new List<ScriptConfig>();
            var allCorruptedFiles = new List<string>();

            var results = sources.Select(source => ScriptConfigReader.LoadWithErrorTracking(source, appSettings)).ToList();
            var el = AppSettingsService.LoadExecutionLog();
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var result in results)
                {
            
                    actions.AddRange(result.Configs.OrderBy(x => x.SourceName).ThenBy(x => x.Name));
                    allCorruptedFiles.AddRange(result.CorruptedFiles);
                }


                foreach (var action in actions)
                {
                    var withMarkers = action.Params.Aggregate
                    (
                        seed: action.Command,
                        func: (string accumulate, ScriptParam source) =>
                            accumulate.Replace("{" + source.Name + "}", "[!@#]{" + source.Name + "}[!@#]")
                    );

                    action.CommandFormatted.AddRange(withMarkers.Split("[!@#]").Select(x =>
                    {
                        var inline = new Run(x);
                        if (x.StartsWith("{"))
                        {

                            inline.Foreground = Brushes.LightGreen;
                            inline.FontWeight = FontWeight.ExtraBold;
                        }

                        return inline;
                    }));

                    // Format InstallCommand if it exists
                    if (!string.IsNullOrWhiteSpace(action.InstallCommand))
                    {
                        var installWithMarkers = action.Params.Aggregate
                        (
                            seed: action.InstallCommand,
                            func: (string accumulate, ScriptParam source) =>
                                accumulate.Replace("{" + source.Name + "}", "[!@#]{" + source.Name + "}[!@#]")
                        );

                        action.InstallCommandFormatted.AddRange(installWithMarkers.Split("[!@#]").Select(x =>
                        {
                            var inline = new Run(x);
                            if (x.StartsWith("{"))
                            {
                                inline.Foreground = Brushes.LightGreen;
                                inline.FontWeight = FontWeight.ExtraBold;
                            }

                            return inline;
                        }));
                    }
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
            
                ExecutionLog.AddRange(el);
                IsLoadingConfig = false;
                if (allCorruptedFiles.Count > 0)
                {
                    ShowCorruptedFilesDialog(allCorruptedFiles);
                }
            });
        });
    }
    
    private async void ShowCorruptedFilesDialog(List<string> corruptedFiles)
    {
        var fileList = string.Join("\n", corruptedFiles.Select(f => $"• {f}"));
        var message = $"The following configuration files are corrupted and were skipped:\n\n{fileList}\n\nPlease check these files for JSON syntax errors.";
        var messageBox = MessageBoxManager.GetMessageBoxStandard(
            "Corrupted Configuration Files", 
            message, 
            icon: MsBox.Avalonia.Enums.Icon.Warning,
            windowStartupLocation: WindowStartupLocation.CenterOwner);
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await messageBox.ShowWindowDialogAsync(desktop.MainWindow);
        }
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
                    ExecuteCommand(command, this.SelectedAction, useSystemShell:false, title, s =>
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
            job.ExecutedCommandFormatted.AddRange(CreateSimpleFormattedCommand(installCommand));
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
        Task.Run(async () =>
        {
            await RefreshInfoAbouAppUpdates();
        });
        Task.Run(async () =>
        {
            await RefreshInfoAboutRepositories();
        });
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
            var pulledWithSuccess = false;
            IReadOnlyList<string> releaseNotes = Array.Empty<string>();
            try
            {
                await Task.Run(async () => (pulledWithSuccess,releaseNotes) = await _configRepositoryUpdater.RefreshRepository(record.Path));
            }
            finally
            {
                record.IsPulling = false;
                if (pulledWithSuccess)
                {

                    OutOfDateConfigRepositories.Remove(record);
                    RefreshSettings();
                    var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard("What's new", string.Join("\r\n", releaseNotes), icon: MsBox.Avalonia.Enums.Icon.Info, windowStartupLocation:WindowStartupLocation.CenterOwner);
                    await messageBoxStandardWindow.ShowAsync();
                }
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
                var controlValue = controlRecord.GetFormattedValue()?.Trim();
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
            
            ExecuteCommand(selectedActionCommand, selectedAction, selectedAction.UseSystemShell);

            // Some audit staff
            var usedParams = HarvestCurrentParameters(vaultPrefixForNewEntries: $"{selectedAction.Name}_{Guid.NewGuid():N}");
            var executionLogAction = new ExecutionLogAction(DateTime.Now,  selectedAction.SourceName, selectedAction.Name, usedParams);
            ExecutionLog.Insert(0, executionLogAction);
            //SelectedRecentExecution = executionLogAction;
            AppSettingsService.UpdateExecutionLog(ExecutionLog.ToList());
        }
        
    }

    private void ExecuteCommand(string command, ScriptConfig selectedAction, bool useSystemShell, string? title = null, Action<string>? onComplete = null)
    {
        var (commandPath, args) = SplitCommandAndArgs(command);
        var envVariables = new Dictionary<string, string?>(selectedAction.EnvironmentVariables);
        var maskedArgs = args;
        
        // Track parameter replacements for formatting with descriptions
        var parameterReplacements = new List<(string paramName, string value, bool masked, string description)>();
        
        foreach (var controlRecord in _controlRecords)
        {
            var controlValue = controlRecord.GetFormattedValue()?.Trim();
            args = args.Replace($"{{{controlRecord.Name}}}", controlValue);
            commandPath = commandPath.Replace($"{{{controlRecord.Name}}}", controlValue);
            var displayValue = controlRecord.MaskingRequired ? "*****" : controlValue;
            maskedArgs = maskedArgs.Replace($"{{{controlRecord.Name}}}", displayValue);
            
            // Find the parameter description from the action's Params
            var paramDescription = selectedAction.Params.FirstOrDefault(p => p.Name == controlRecord.Name)?.Description ?? string.Empty;
            
            parameterReplacements.Add((controlRecord.Name, displayValue, controlRecord.MaskingRequired, paramDescription));

            foreach (var (key, val) in envVariables)
            {
                if(string.IsNullOrWhiteSpace(val) == false)
                    envVariables[key] = val.Replace($"{{{controlRecord.Name}}}", controlValue);
            }
        }

        var executedCommand = $"{commandPath} {maskedArgs}";
        
        // Create formatted version with highlighted parameter values
        var formattedCommand = executedCommand;
        foreach (var (paramName, value, masked, description) in parameterReplacements)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                formattedCommand = formattedCommand.Replace(value, $"[!@#]{value}[!@#]");
            }
        }
        
        var executedCommandFormatted = formattedCommand.Split("[!@#]").Select(x =>
        {
            var inline = new Run(x);
            // Check if this is a parameter value (not the original text)
            var matchingParam = parameterReplacements.FirstOrDefault(p => p.value == x && !string.IsNullOrWhiteSpace(x));
            if (matchingParam != default)
            {
                inline.Foreground = Brushes.LightGreen;
                inline.FontWeight = FontWeight.ExtraBold;
            }
            return inline;
        }).ToList();

        var job = new RunningJobViewModel
        {
            Tile = $"#{jobCounter++} {title ?? selectedAction.Name}",
            ExecutedCommand = executedCommand,
            EnvironmentVariables = envVariables
        };
        job.ExecutedCommandFormatted.AddRange(executedCommandFormatted);
        
        this.RunningJobs.Add(job);
        SelectedRunningJob = job;

        if (onComplete != null)
        {
            job.ExecutionCompleted += (sender, args) => onComplete(job.RawOutput);
        }
        
        job.RunJob(commandPath, args, selectedAction.WorkingDirectory, selectedAction.InteractiveInputs, selectedAction.Troubleshooting, useSystemShell);
    }

    public ObservableCollection<ExecutionLogAction> ExecutionLog { get; set; } = new ();
    
    // Grouped execution log with date dividers
    private readonly ObservableAsPropertyHelper<IEnumerable<ExecutionLogItemBase>> _executionLogGrouped;
    public IEnumerable<ExecutionLogItemBase> ExecutionLogGrouped => _executionLogGrouped.Value;
    
    // Available dates for date picker
    private readonly ObservableAsPropertyHelper<IEnumerable<DateGroupInfo>> _availableDates;
    public IEnumerable<DateGroupInfo> AvailableDates => _availableDates.Value;
    
    // Date picker visibility
    private bool _isDatePickerVisible;
    public bool IsDatePickerVisible
    {
        get => _isDatePickerVisible;
        set => this.RaiseAndSetIfChanged(ref _isDatePickerVisible, value);
    }
    
    // Store reference to the ListBox for scrolling
    public Action<DateTime>? ScrollToDateAction { get; set; }
    
    private readonly ObservableAsPropertyHelper<IEnumerable<ExecutionLogAction>>  _executionLogForCurrent;
    public IEnumerable<ExecutionLogAction>  ExecutionLogForCurrent => _executionLogForCurrent.Value;
    


    public ExecutionLogAction SelectedRecentExecution
    {
        get => _selectedRecentExecution;
        set=> this.RaiseAndSetIfChanged(ref _selectedRecentExecution, value);
    }

    private ExecutionLogAction _selectedRecentExecution;
    
    public ExecutionLogItemBase? SelectedExecutionLogItem
    {
        get => _selectedExecutionLogItem;
        set
        {
            // Ignore selection of date headers - only allow action items to be selected
            if (value is ExecutionLogDateHeader)
            {
                // Reset selection to null for date headers
                this.RaiseAndSetIfChanged(ref _selectedExecutionLogItem, null);
                return;
            }
            
            this.RaiseAndSetIfChanged(ref _selectedExecutionLogItem, value);
            // When an ExecutionLogItemAction is selected, set the underlying ExecutionLogAction
            if (value is ExecutionLogItemAction itemAction)
            {
                SelectedRecentExecution = itemAction.Action;
            }
        }
    }

    private ExecutionLogItemBase? _selectedExecutionLogItem;
    private readonly ConfigRepositoryUpdater _configRepositoryUpdater;


    private void AddExecutionAudit(ScriptConfig selectedAction)
    {
        AppSettingsService.UpdateRecent(recent =>
        {
            var actionId = new ActionId(selectedAction.SourceName ?? string.Empty, selectedAction.Name, SelectedArgumentSet?.Description ?? "<default>");
            recent[$"{actionId.SourceName}__{actionId.ActionName}__{actionId.ParameterSet}"] = new RecentAction(actionId, DateTime.UtcNow);
        });
    }

    /// <summary>
    /// Scrolls to the specified date in the execution log
    /// </summary>
    public void ScrollToDate(DateTime date)
    {
        IsDatePickerVisible = false;
        ScrollToDateAction?.Invoke(date);
    }

    public void CloseJob(object arg)
    {
        if (arg is RunningJobViewModel job)
        {
            job.CancelExecution();
            RunningJobs.Remove(job);
        }
    }

    public void CloseAllFinished()
    {
        var finishedJobs = RunningJobs.Where(job => job.Status != RunningJobStatus.Running).ToList();
        foreach (var job in finishedJobs)
        {
            job.CancelExecution();
            RunningJobs.Remove(job);
        }
    }

    public void CloseAllFailed()
    {
        var failedJobs = RunningJobs.Where(job => job.Status == RunningJobStatus.Failed).ToList();
        foreach (var job in failedJobs)
        {
            job.CancelExecution();
            RunningJobs.Remove(job);
        }
    }

    public void CloseAllCancelled()
    {
        var cancelledJobs = RunningJobs.Where(job => job.Status == RunningJobStatus.Cancelled).ToList();
        foreach (var job in cancelledJobs)
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
    
    private static InlineCollection CreateSimpleFormattedCommand(string command)
    {
        var collection = new InlineCollection();
        collection.Add(new Run(command));
        return collection;
    }
}

public record RecentAction(ActionId ActionId, DateTime Timestamp);

public record ActionId(string SourceName, string ActionName, string ParameterSet);

public class ScriptConfigGroupWrapper
{
    public string Name { get; set; }
    public IEnumerable<TaggedScriptConfig> Children { get; set; }
}

public record TaggedScriptConfig(string Tag, string Name, ScriptConfig Config);
