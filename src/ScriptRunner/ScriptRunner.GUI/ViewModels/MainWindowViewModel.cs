using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ScriptReader;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string GetProductFullVersion()
    {
        var assemblyVersion = this.GetType().Assembly.GetName().Version;
        return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
    }

    public class ReleaseResponse
    {
        public string tag_name { get; set; }
    }

    public async Task<bool> CheckIsNewerVersionAvailable()
    {
        using var httpClient = new HttpClient();
        var currentProductVersionRaw = GetProductFullVersion();
        try
        {
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ScriptRunner", currentProductVersionRaw));
            var response = await httpClient.GetAsync("https://api.github.com/repos/cezarypiatek/ScriptRunnerPOC/releases/latest").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReleaseResponse>(payload);
                if (string.IsNullOrWhiteSpace(result?.tag_name) == false)
                {
                    var latestVersion = Version.Parse(result.tag_name);

                    var currentVersion = Version.Parse(currentProductVersionRaw);
                    return latestVersion > currentVersion;
                }
            }
        }
        catch (Exception e)
        {
            return false;
        }

        return  false;
    }

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




    public MainWindowViewModel()
    {
        Task.Run(async () =>
        {
            var isNewerVersion = await CheckIsNewerVersionAvailable();
            if (isNewerVersion)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsNewerVersionAvailable = true;
                });
            }
        });
        ActionParametersPanel = new ObservableCollection<IPanel>();
        Actions = new ObservableCollection<ScriptConfig>();
        ParameterSetsForCurrentAction = new ObservableCollection<ArgumentSet>();
        BuildUi();
    }

    public void CheckForUpdates()
    {
        OpenWebsite(@"https://github.com/cezarypiatek/ScriptRunnerPOC/releases/");
    }


    public static void OpenWebsite(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url.Replace("&", "^&"),
            UseShellExecute = true
        });
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
        Actions.Clear();
        foreach (var action in  sources.SelectMany(x=> ScriptConfigReader.Load(x)).OrderBy(x=>x.SourceName).ThenBy(x=>x.Name))
        {
            Actions.Add(action);
        }

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
    private bool _selectedActionInstalled;

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
            job.RunJob(commandPath, args, selectedAction.InstallCommandWorkingDirectory);
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
            job.RunJob(commandPath, args, selectedAction.WorkingDirectory);
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
