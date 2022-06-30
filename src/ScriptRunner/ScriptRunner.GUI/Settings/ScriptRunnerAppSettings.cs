using System;
using System.Collections.Generic;
using ScriptRunner.GUI.Settings;

namespace ScriptRunner.GUI;

public class ScriptRunnerAppSettings
{
    public LayoutSettings? Layout { get; set; }
    public Dictionary<string, CommandInstallationStatus> InstalledActions { get; set; }
    public List<ConfigScriptEntry>? ConfigScripts { get; set; }
}

public record ConfigScriptEntry
{
    public string Name { get; set; }
    public string Path { get; set; }
    public ConfigScriptType Type { get; set; }
    public bool Recursive { get; set; }
}

public class CommandInstallationStatus
{
    public bool IsInstalled { get; set; }
}