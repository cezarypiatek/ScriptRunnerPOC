using System.Collections.Generic;

namespace ScriptRunner.GUI.Settings;

public class ScriptRunnerAppSettings
{
    public LayoutSettings? Layout { get; set; }
    public Dictionary<string, CommandInstallationStatus> InstalledActions { get; set; }
    public List<ConfigScriptEntry>? ConfigScripts { get; set; }
    public List<VaultBinding> VaultBindings { get; set; }
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

public class VaultBinding
{
    public string ActionName { get; set; }
    public string ParameterName { get; set; }
    public string VaultKey { get; set; }
}