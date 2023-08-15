using ScriptRunner.GUI.ScriptConfigs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ScriptRunner.GUI.ViewModels;
using static ScriptRunner.GUI.ViewModels.MainWindowViewModel;

namespace ScriptRunner.GUI.Settings;

public class AppSettingsService
{
    private static string ExecutionLogFileName = "ExecutionLog.json";

    public static ScriptRunnerAppSettings Load()
    {
        return Load<ScriptRunnerAppSettings>();
    }
    
    
    public static List<ExecutionLogAction> LoadExecutionLog()
    {
        return Load<List<ExecutionLogAction>>(ExecutionLogFileName);
    }
    
    private static T Load<T>(string? fileName = null) where T : new()
    {
        try
        {
            var settingsPath = GetSettingsPath(fileName);
            if (File.Exists(settingsPath))
            {
                var payload = File.ReadAllText(settingsPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(payload) == false && JsonSerializer.Deserialize<T>(payload) is { } settings)
                {
                    return settings;
                }
            }
        }
        catch
        {
            
        }
        return new T();
    }

    private static void Save<T>(T settings, string? fileName = null)
    {
        try
        {
            var payload = JsonSerializer.Serialize(settings);
            var settingsPath = GetSettingsPath(fileName);
            File.WriteAllText(settingsPath, payload, Encoding.UTF8);
        }
        catch
        {
        }
    }

    public static void MarkActionAsInstalled(string actionName)
    {
        var allSettings = AppSettingsService.Load();
        allSettings.InstalledActions ??= new Dictionary<string, CommandInstallationStatus>();
        allSettings.InstalledActions[actionName] = new CommandInstallationStatus()
        {
            IsInstalled = true
        };
        Save(allSettings);
    }

  

    public static void UpdateLayoutSettings(Action<LayoutSettings> updateSettings)
    {
        var allSettings = AppSettingsService.Load();
        allSettings.Layout ??= new LayoutSettings();
        updateSettings(allSettings.Layout);
        Save(allSettings);
        Debug.WriteLine($"Width: {allSettings.Layout.Width}, Height: {allSettings.Layout.Height}, L: {allSettings.Layout.ActionsPanelWidth}, M: {allSettings.Layout.RunningJobsPanelHeight}");
    }
    
    public static void UpdateExecutionLog(List<ExecutionLogAction> executionLog)
    {
        Save(executionLog, ExecutionLogFileName);
    }
    
    public static void UpdateRecent(Action<Dictionary<string, RecentAction>> updateSettings)
    {
        var allSettings = AppSettingsService.Load();
        allSettings.Recent ??= new();
        updateSettings(allSettings.Recent);
        Save(allSettings);
    }
    
    public static void UpdateDefaultOverrides(ActionDefaultOverrides overrides)
    {
        var allSettings = AppSettingsService.Load();
        allSettings.DefaultOverrides ??= new ();

        var existingOverride = allSettings.DefaultOverrides.FirstOrDefault(x => x.ActionName == overrides.ActionName);
        if (existingOverride != null)
        {
            allSettings.DefaultOverrides.Remove(existingOverride);
        }
        allSettings.DefaultOverrides.Add(overrides);
        Save(allSettings);
    }
    
    public static void UpdateExtraParameterSet(ActionExtraPredefinedParameterSet parameterSet)
    {
        var allSettings = AppSettingsService.Load();
        allSettings.ExtraParameterSets ??= new ();

        var existingOne = allSettings.ExtraParameterSets.FirstOrDefault(x => x.ActionName == parameterSet.ActionName && x.Description == parameterSet.Description);
        if (existingOne != null)
        {
            allSettings.ExtraParameterSets.Remove(existingOne);
        }
        allSettings.ExtraParameterSets.Add(parameterSet);
        Save(allSettings);
    }

    public static Dictionary<string, string>? TryGetDefaultOverrides(string actionName)
    {
        var allSettings = AppSettingsService.Load();
        if (allSettings.DefaultOverrides?.FirstOrDefault(x => x.ActionName == actionName) is { Defaults: var overrides } )
        {
            return overrides;
        }
        return null;
    }

    public static void RemoveScriptConfig(ConfigScriptEntry entry)
    {
        var allSettings = Load();
        allSettings.ConfigScripts?.Remove(entry);
        Save(allSettings);
    }


    public static void UpdateScriptConfigs(IEnumerable<ConfigScriptEntry> configScripts)
    {
        var allSettings = Load();
        allSettings.ConfigScripts = configScripts.ToList();
        Save(allSettings);
    }

    public static void UpdateVaultBindings(VaultBinding binding)
    {
        var allSettings = Load();
        allSettings.VaultBindings ??= new List<VaultBinding>();
        var existingBinding = allSettings.VaultBindings.FirstOrDefault(x => x.ActionName == binding.ActionName && x.ParameterName == binding.ParameterName);
        if (existingBinding != null)
        {
            existingBinding.VaultKey = binding.VaultKey;
        }
        else
        {
            allSettings.VaultBindings.Add(binding);
        }

        Save(allSettings);
    }

    private static string GetSettingsPath(string? fileName = null)
    {
        return GetSettingsPathFor(fileName ?? "settings.json");
    }

    public static string GetSettingsPathFor(string settingsFile)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScriptRunner");
        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
        }

        return Path.Combine(path, settingsFile);
    }
}