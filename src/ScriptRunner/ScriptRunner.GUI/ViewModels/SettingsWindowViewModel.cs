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

    /// <summary>
    /// Contains records of directories that may contain configuration scripts
    /// </summary>
    private ObservableCollection<ConfigScriptDirectoryRow> _configScriptDirectories;

    public ObservableCollection<ConfigScriptDirectoryRow> ConfigScriptDirectories
    {
        get => _configScriptDirectories;
        private set => this.RaiseAndSetIfChanged(ref _configScriptDirectories, value);
    }

    public SettingsWindowViewModel()
    {
        var configScripts = AppSettingsService.Load().ConfigScripts ?? new List<string>();
        var configScriptsDirectories = AppSettingsService.Load().ConfigScriptsDirectories ?? new List<ConfigScriptDirectorySetting>();
        ConfigScriptFiles = new ObservableCollection<ConfigScriptFileRow>(configScripts.Select(path => new ConfigScriptFileRow(path, SaveConfigScripts)));
        ConfigScriptDirectories = new ObservableCollection<ConfigScriptDirectoryRow>(configScriptsDirectories
            .Select(entry => new ConfigScriptDirectoryRow(entry.Path, entry.Recursive, UpdateScriptConfigsDirectories)));
    }

    public void AddNewConfigScriptRow()
    {
        ConfigScriptFiles.Add(new ConfigScriptFileRow(string.Empty, SaveConfigScripts));
    }

    public void AddNewConfigScriptDirectoryRow()
    {
        ConfigScriptDirectories.Add(new ConfigScriptDirectoryRow(string.Empty, false, UpdateScriptConfigsDirectories));
    }

    public void RemoveConfigScript(ConfigScriptFileRow configScriptFileRow)
    {
        AppSettingsService.RemoveScriptConfig(configScriptFileRow.Path);
        ConfigScriptFiles.Remove(configScriptFileRow);
    }
    public void RemoveConfigScriptDirectory(ConfigScriptDirectoryRow configScriptDirectoryRow)
    {
        AppSettingsService.RemoveScriptConfigDirectory(configScriptDirectoryRow.Path);
        ConfigScriptDirectories.Remove(configScriptDirectoryRow);
    }

    private void SaveConfigScripts()
    {
        AppSettingsService.UpdateScriptConfigs(ConfigScriptFiles.Select(q => q.Path));
    }
    
    private void UpdateScriptConfigsDirectories()
    {
        AppSettingsService.UpdateScriptConfigsDirectories(ConfigScriptDirectories.Select(q => (q.Path, q.Recursive)));
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

public class ConfigScriptDirectoryRow
{
    public string Path { get; set; }

    // TODO: Check if file path is correct and mark this flag
    public bool Exists { get; set; }
    public bool Recursive { get; set; }

    public readonly Action UpdateConfigScriptEntry;

    public ConfigScriptDirectoryRow(string path, bool recursive, Action updateConfigScriptEntry)
    {
        Path = path;
        Recursive = recursive;
        UpdateConfigScriptEntry = updateConfigScriptEntry;
    }

    private void OnDirectoryPicked(FilePickedArgs args)
    {
        Path = args.Path;
        UpdateConfigScriptEntry.Invoke();
    }

    private void OnCheckboxChanged(bool value)
    {
        Recursive = value;
        UpdateConfigScriptEntry.Invoke();
    }
}