using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Settings;

internal static class ConfigScriptEntryMapper
{
    internal static ConfigScriptEntry Map(ConfigScriptRow configScriptRow)
    {
        return new ConfigScriptEntry
        {
            Name = configScriptRow.Name,
            Path = configScriptRow.Path,
            Type = configScriptRow.Type,
            Recursive = configScriptRow.Recursive
        };
    }
}