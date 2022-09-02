using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

interface IAutoParameterBuilder
{
    string Build(ScriptParam param);
}