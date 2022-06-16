using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Contains records of configuration scripts loaded from settings file
    /// </summary>
    private ObservableCollection<ConfigScriptFileRow> _configScriptFiles;

    public ObservableCollection<ConfigScriptFileRow> ConfigScriptFiles
    {
        get => _configScriptFiles;
        private set => this.RaiseAndSetIfChanged(ref _configScriptFiles, value);
    }

    public SettingsWindowViewModel()
    {
        var configScripts = AppSettingsService.Load().ConfigScripts ?? new List<string>();
        ConfigScriptFiles =
            new ObservableCollection<ConfigScriptFileRow>(configScripts.Select(path => new ConfigScriptFileRow(path, SaveConfigScripts)));
    }

    public void AddNewConfigScriptRow()
    {
        ConfigScriptFiles.Add(new ConfigScriptFileRow(string.Empty, SaveConfigScripts));
    }

    public void RemoveConfigScript(ConfigScriptFileRow configScriptFileRow)
    {
        AppSettingsService.RemoveScriptConfig(configScriptFileRow.Path);
        ConfigScriptFiles.Remove(configScriptFileRow);
    }

    private void SaveConfigScripts()
    {
        AppSettingsService.UpdateScriptConfigs(ConfigScriptFiles.Select(q => q.Path));
    }
}

public class ConfigScriptFileRow
{
    public string Path { get; set; }

    // TODO: Check if file path is correct and mark this flag
    public bool Exists { get; set; }

    public Action UpdateConfigScriptEntry;

    public ConfigScriptFileRow(string path, Action updateConfigScriptEntry)
    {
        Path = path;
        UpdateConfigScriptEntry = updateConfigScriptEntry;
    }

    private void OnFilePicked(FilePickedArgs args)
    {
        Path = args.Path;
        UpdateConfigScriptEntry.Invoke();
    }
}