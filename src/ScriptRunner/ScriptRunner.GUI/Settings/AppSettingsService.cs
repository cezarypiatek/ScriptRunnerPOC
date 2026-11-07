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
            return LoadForUpdate<T>(fileName);
        }
        catch
        {
            return new T();
        }
    }

    private static T LoadForUpdate<T>(string? fileName = null) where T : new()
    {
        var settingsPath = GetSettingsPath(fileName);
        if (!File.Exists(settingsPath))
        {
            return new T();
        }

        var payload = File.ReadAllText(settingsPath, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidDataException($"Settings file '{settingsPath}' is empty.");
        }

        return JsonSerializer.Deserialize<T>(payload)
               ?? throw new InvalidDataException($"Settings file '{settingsPath}' is invalid.");
    }

    private static void UpdateSettingsBestEffort(Action<ScriptRunnerAppSettings> update)
    {
        try
        {
            var settings = LoadForUpdate<ScriptRunnerAppSettings>();
            update(settings);
            SaveForUpdate(settings);
        }
        catch
        {
            // Noncritical UI preferences remain best-effort, but never replace a corrupt file
            // with the empty fallback returned by the public tolerant loader.
        }
    }

    private static void SaveForUpdate<T>(T settings, string? fileName = null)
    {
        var payload = JsonSerializer.Serialize(settings);
        var settingsPath = GetSettingsPath(fileName);
        var temporaryPath = $"{settingsPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, payload, Encoding.UTF8);
            File.Move(temporaryPath, settingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static void MarkActionAsInstalled(string actionName)
    {
        UpdateSettingsBestEffort(allSettings =>
        {
            allSettings.InstalledActions ??= new Dictionary<string, CommandInstallationStatus>();
            allSettings.InstalledActions[actionName] = new CommandInstallationStatus
            {
                IsInstalled = true
            };
        });
    }

    public static void SetActionFavorite(string actionKey, bool isFavorite)
    {
        UpdateSettingsBestEffort(allSettings =>
        {
            allSettings.FavoriteActions ??= new List<string>();
            allSettings.FavoriteActions.RemoveAll(key =>
                string.Equals(key, actionKey, StringComparison.Ordinal));
            if (isFavorite)
            {
                allSettings.FavoriteActions.Add(actionKey);
            }
        });
    }

  

    public static void UpdateLayoutSettings(Action<LayoutSettings> updateSettings)
    {
        UpdateSettingsBestEffort(allSettings =>
        {
            allSettings.Layout ??= new LayoutSettings();
            updateSettings(allSettings.Layout);
            Debug.WriteLine($"Width: {allSettings.Layout.Width}, Height: {allSettings.Layout.Height}, L: {allSettings.Layout.ActionsPanelWidth}, M: {allSettings.Layout.RunningJobsPanelHeight}");
        });
    }
    
    public static void UpdateExecutionLog(List<ExecutionLogAction> executionLog)
    {
        // Validate an existing file before replacing it so a recoverable corrupt log is not
        // silently destroyed by the tolerant startup loader.
        _ = LoadForUpdate<List<ExecutionLogAction>>(ExecutionLogFileName);
        SaveForUpdate(executionLog, ExecutionLogFileName);
    }
    
    public static void UpdateRecent(Action<Dictionary<string, RecentAction>> updateSettings)
    {
        UpdateSettingsBestEffort(allSettings =>
        {
            allSettings.Recent ??= new();
            updateSettings(allSettings.Recent);
        });
    }
    
    public static void UpdateDefaultOverrides(ActionDefaultOverrides overrides)
    {
        var allSettings = LoadForUpdate<ScriptRunnerAppSettings>();
        allSettings.DefaultOverrides ??= new ();

        allSettings.DefaultOverrides.RemoveAll(x =>
            x.ActionName == overrides.ActionName &&
            x.SourceName == overrides.SourceName);
        allSettings.DefaultOverrides.Add(overrides);
        SaveForUpdate(allSettings);
    }
    
    public static void UpdateExtraParameterSet(ActionExtraPredefinedParameterSet parameterSet)
    {
        var allSettings = LoadForUpdate<ScriptRunnerAppSettings>();
        allSettings.ExtraParameterSets ??= new ();

        allSettings.ExtraParameterSets.RemoveAll(x =>
            x.ActionName == parameterSet.ActionName &&
            x.SourceName == parameterSet.SourceName &&
            x.Description == parameterSet.Description);
        allSettings.ExtraParameterSets.Add(parameterSet);
        SaveForUpdate(allSettings);
    }

    public static Dictionary<string, string>? TryGetDefaultOverrides(string actionName, string? sourceName)
    {
        var allSettings = AppSettingsService.Load();
        var matchingOverrides = SettingsScopeResolver.FindBest(
            allSettings.DefaultOverrides?.Where(x => x.ActionName == actionName)
            ?? Enumerable.Empty<ActionDefaultOverrides>(),
            sourceName,
            x => x.SourceName);
        if (matchingOverrides is { Defaults: var overrides })
        {
            return overrides;
        }
        return null;
    }

    public static void RemoveScriptConfig(ConfigScriptEntry entry)
    {
        UpdateSettingsBestEffort(allSettings => allSettings.ConfigScripts?.Remove(entry));
    }


    public static void UpdateMcpServerSettings(McpServerSettings mcpSettings)
    {
        UpdateSettingsBestEffort(allSettings => allSettings.McpServer = mcpSettings);
    }

    public static void UpdateScriptConfigs(IEnumerable<ConfigScriptEntry> configScripts)
    {
        var entries = configScripts.ToList();
        UpdateSettingsBestEffort(allSettings => allSettings.ConfigScripts = entries);
    }

    public static void UpdateVaultBindings(VaultBinding binding)
    {
        var allSettings = LoadForUpdate<ScriptRunnerAppSettings>();
        allSettings.VaultBindings ??= new List<VaultBinding>();
        allSettings.VaultBindings.RemoveAll(x =>
            x.ActionName == binding.ActionName &&
            x.SourceName == binding.SourceName &&
            x.ParameterName == binding.ParameterName &&
            x.ParameterSetName == null);
        allSettings.VaultBindings.Add(binding);

        SaveForUpdate(allSettings);
    }

    public static void UpdateVaultBindingsForParameterSet(VaultBinding binding)
    {
        var allSettings = LoadForUpdate<ScriptRunnerAppSettings>();
        allSettings.VaultBindings ??= new List<VaultBinding>();
        allSettings.VaultBindings.RemoveAll(x =>
            x.ActionName == binding.ActionName &&
            x.SourceName == binding.SourceName &&
            x.ParameterName == binding.ParameterName &&
            x.ParameterSetName == binding.ParameterSetName);
        allSettings.VaultBindings.Add(binding);

        SaveForUpdate(allSettings);
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
