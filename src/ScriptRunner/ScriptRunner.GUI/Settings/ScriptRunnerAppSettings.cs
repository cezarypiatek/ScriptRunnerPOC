using System.Collections.Generic;

namespace ScriptRunner.GUI;

public class ScriptRunnerAppSettings
{
    public LayoutSettings? Layout { get; set; }
    public Dictionary<string, CommandInstallationStatus> InstalledActions { get; set; }
    
}

public class CommandInstallationStatus
{
    public bool IsInstalled { get; set; }
}