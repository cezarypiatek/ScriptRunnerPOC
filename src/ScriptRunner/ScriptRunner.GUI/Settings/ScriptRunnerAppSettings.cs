using System.Collections.Generic;

namespace ScriptRunner.GUI.Settings;

public class ScriptRunnerAppSettings
{
    public LayoutSettings? Layout { get; set; }
    public Dictionary<string, CommandInstallationStatus> InstalledActions { get; set; }
    public List<ConfigScriptEntry>? ConfigScripts { get; set; }
    public List<VaultBinding> VaultBindings { get; set; }
    public List<ActionDefaultOverrides> DefaultOverrides { get; set; }
    public List<ActionExtraPredefinedParameterSet> ExtraParameterSets { get; set; }
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

public class ActionDefaultOverrides
{
    public string ActionName { get; set; }
    public Dictionary<string,string> Defaults { get; set; }
}
public class ActionExtraPredefinedParameterSet
{
    public string ActionName { get; set; }
    public string Description { get; set; }
    public Dictionary<string,string> Arguments { get; set; }
}