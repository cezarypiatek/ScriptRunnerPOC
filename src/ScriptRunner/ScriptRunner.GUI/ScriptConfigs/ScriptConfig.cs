using System.Collections.Generic;

namespace ScriptRunner.GUI.ScriptConfigs;

public class ScriptConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Command { get; set; }
    public IEnumerable<ScriptParam> Params { get; set; }
}

public class ScriptParam
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ParamType Type { get; set; }
    public PromptType Prompt { get; set; }
}