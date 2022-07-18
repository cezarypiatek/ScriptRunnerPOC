using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;

namespace ScriptRunner.GUI.ScriptReader;

public static class ScriptConfigReader
{
    public static IEnumerable<ScriptConfig> Load(ConfigScriptEntry source)
    {
        if (source.Type == ConfigScriptType.File)
        {
            foreach (var scriptConfig in LoadFileSource(source.Path))
            {
                yield return scriptConfig;
            }
            yield break;
        }

        if (source.Type == ConfigScriptType.Directory)
        {
            foreach (var file in Directory.EnumerateFiles(source.Path, "*.json", source.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                foreach (var scriptConfig in LoadFileSource(file))
                {
                    yield return scriptConfig;
                }
            }
            yield break;
        }
    }

    private static IEnumerable<ScriptConfig> LoadFileSource(string fileName)
    {
        if (!File.Exists(fileName)) return Array.Empty<ScriptConfig>();

        try
        {
            var jsonString = File.ReadAllText(fileName);

            if (jsonString.Contains("ScriptRunnerSchema.json") == false)
            {
                return Array.Empty<ScriptConfig>();
            }

            var scriptConfig = JsonSerializer.Deserialize<ActionsConfig>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = {new PromptTypeJsonConverter(), new ParamTypeJsonConverter()}
            })!;


            foreach (var action in scriptConfig.Actions)
            {
                action.Source = fileName;
                var defaultSet = new ArgumentSet()
                {
                    Description = "<default>"
                };
                foreach (var param in action.Params)
                {
                    defaultSet.Arguments[param.Name] = param.Default;
                }

                foreach (var set in action.PredefinedArgumentSets.Where(x => x.FallbackToDefault))
                {
                    foreach (var (key, val) in defaultSet.Arguments)
                    {
                        if (set.Arguments.ContainsKey(key) == false)
                        {
                            set.Arguments[key] = val;
                        }
                    }
                }

                action.PredefinedArgumentSets.Insert(0, defaultSet);
                if (string.IsNullOrWhiteSpace(action.WorkingDirectory))
                {
                    action.WorkingDirectory = Path.GetDirectoryName(fileName);
                }

                if (string.IsNullOrWhiteSpace(action.InstallCommandWorkingDirectory))
                {
                    action.InstallCommandWorkingDirectory = Path.GetDirectoryName(fileName);
                }
            }

            return scriptConfig.Actions;
        }
        catch
        {
            return Enumerable.Empty<ScriptConfig>();
        }
    }
}