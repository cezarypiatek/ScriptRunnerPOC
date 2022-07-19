using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ScriptRunner.GUI.Settings;

public class AppSettingsService
{
    public static ScriptRunnerAppSettings Load()
    {
        try
        {
            var settingsPath = GetSettingsPath();
            if (File.Exists(settingsPath))
            {
                var payload = File.ReadAllText(settingsPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(payload) == false && JsonSerializer.Deserialize<ScriptRunnerAppSettings>(payload) is { } settings)
                {
                    return settings;
                }
            }
        }
        catch
        {
            
        }
        return new ScriptRunnerAppSettings();
    }

    private static void Save(ScriptRunnerAppSettings settings)
    {
        try
        {
            var payload = JsonSerializer.Serialize(settings);
            var settingsPath = GetSettingsPath();
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

    private static string GetSettingsPath()
    {
        return GetSettingsPathFor("settings.json");
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