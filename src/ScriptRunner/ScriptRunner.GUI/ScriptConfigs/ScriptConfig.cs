using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace ScriptRunner.GUI.ScriptConfigs;

public class ScriptConfig
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Command { get; set; }
    public string? WorkingDirectory { get; set; }
    public IEnumerable<ScriptParam> Params { get; set; }
}

public class ScriptParam
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ParamType Type { get; set; }
    public PromptType Prompt { get; set; }
    public string Default { get; set; }
    public Dictionary<string, string> PromptSettings { get; set; } = new();

    public bool GetPromptSettings(string name, [NotNullWhen(true)] out string? value)
    {
        return PromptSettings.TryGetValue(name, out value);
    }

}