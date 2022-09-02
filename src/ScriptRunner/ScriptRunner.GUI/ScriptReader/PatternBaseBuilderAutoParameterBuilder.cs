using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

class PatternBaseBuilderAutoParameterBuilder : IAutoParameterBuilder
{
    private readonly string _pattern;

    public PatternBaseBuilderAutoParameterBuilder(string pattern)
    {
        _pattern = pattern;
    }

    public string Build(ScriptParam param)
    {
        var actionAutoParameterBuilderPattern = param.AutoParameterBuilderPattern ?? _pattern ?? string.Empty;
        return actionAutoParameterBuilderPattern
            .Replace("{name}", param.Name)
            .Replace("{value}", $"{{{param.Name}}}");
    }
}