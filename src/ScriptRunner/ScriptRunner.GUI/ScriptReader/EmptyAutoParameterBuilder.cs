using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

class EmptyAutoParameterBuilder : IAutoParameterBuilder
{
    public static readonly IAutoParameterBuilder Instance = new EmptyAutoParameterBuilder();

    public string Build(ScriptParam param) => string.Empty;
    
}