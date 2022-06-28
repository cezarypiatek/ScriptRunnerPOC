using System.Collections.Generic;

namespace ScriptRunner.GUI;

public class ScriptRunnerAppSettings
{
    public LayoutSettings? Layout { get; set; }
    public Dictionary<string, CommandInstallationStatus> InstalledActions { get; set; }
    public List<string>? ConfigScripts { get; set; }
    public List<ConfigScriptDirectorySetting>? ConfigScriptsDirectories { get; set; }
}

public class ConfigScriptDirectorySetting
{
    public string Path { get; set; }
    public bool Recursive { get; set; }
}

public class CommandInstallationStatus
{
    public bool IsInstalled { get; set; }
}