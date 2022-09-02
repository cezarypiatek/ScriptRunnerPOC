using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

class PowerShellAutoParameterBuilder : IAutoParameterBuilder
{
    public static readonly IAutoParameterBuilder Instance = new PowerShellAutoParameterBuilder();

    public string Build(ScriptParam param)
    {
        if (param.Prompt == PromptType.Checkbox)
        {
            return $"-{param.Name}:{{{param.Name}}}";
        }

        return $"-{param.Name} '{{{param.Name}}}'";
    }
}