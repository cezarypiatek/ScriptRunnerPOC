using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ReactiveUI;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Contains records of configuration scripts loaded from settings file
    /// </summary>
    private ObservableCollection<ConfigScriptRow> _configScriptFiles;

    public ObservableCollection<ConfigScriptRow> ConfigScriptFiles
    {
        get => _configScriptFiles;
        private set => this.RaiseAndSetIfChanged(ref _configScriptFiles, value);
    }

    public SettingsWindowViewModel()
    {
        var configScripts = AppSettingsService.Load().ConfigScripts ?? new List<ConfigScriptEntry>();
        ConfigScriptFiles = new ObservableCollection<ConfigScriptRow>(
            configScripts.Select(entry => new ConfigScriptRow(entry)));
    }

    public void AddNewConfigScriptRow()
    {
        ConfigScriptFiles.Add(new ConfigScriptRow());
    }

    public async Task AddNewConfigScriptFromDir()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime {MainWindow: {}sourceWindow})
        {

            var result = await sourceWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false
            });

            if (result.Count == 1)
            {
                ConfigScriptFiles.Add(new ConfigScriptRow()
                {
                    Type = ConfigScriptType.Directory,
                    Path = result[0].Path.LocalPath,
                    Recursive = true
                });
            }
        }
    }
    
    public async Task AddNewConfigScriptFromFile()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime {MainWindow: {}sourceWindow})
        {

            var result = await sourceWindow.StorageProvider.OpenFilePickerAsync(new ()
            {
                AllowMultiple = false
            });

            if (result.Count == 1)
            {
                ConfigScriptFiles.Add(new ConfigScriptRow()
                {
                    Type = ConfigScriptType.File,
                    Path = result[0].Path.LocalPath,
                    Recursive = false
                });
            }
        }
    }
    
    public void RemoveConfigScript(object arg)
    {
        if (arg is ConfigScriptRow configScriptRow)
        {
            AppSettingsService.RemoveScriptConfig(ConfigScriptEntryMapper.Map(configScriptRow));
            ConfigScriptFiles.Remove(configScriptRow);
        }
    }

    public void SaveConfigScripts()
    {
        AppSettingsService.UpdateScriptConfigs(ConfigScriptFiles.Select(ConfigScriptEntryMapper.Map));
    }
    
}

public class ConfigScriptRow
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool Recursive { get; set; }
    public ConfigScriptType Type { get; set; }


    internal ConfigScriptRow()
    {
        Name = string.Empty;
        Path = string.Empty;
        Type = ConfigScriptType.File;
        Recursive = false;

    }

    internal ConfigScriptRow(ConfigScriptEntry configScriptEntry)
    {
        Name = configScriptEntry.Name;
        Path = configScriptEntry.Path;
        Type = configScriptEntry.Type;
        Recursive = configScriptEntry.Recursive;

    }

    public void OnFilePicked(object args)
    {
        if (args is FilePickedArgs {Path: var path})
        {
            Path = path;
        }
    }

    public void OnDirectoryPicked(object args)
    {
        if (args is FilePickedArgs {Path: var path})
        {
            Path = path;
        }
    }

}
