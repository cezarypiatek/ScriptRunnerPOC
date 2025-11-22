using System.Collections.Generic;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

public class ConfigLoadResult
{
    public List<ScriptConfig> Configs { get; } = new();
    public List<string> CorruptedFiles { get; } = new();
}

