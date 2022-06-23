using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

public static class ScriptConfigReader
{
    public static IEnumerable<ScriptConfig> Load(string fileName)
    {
        
        var jsonString = File.ReadAllText(fileName);
        var scriptConfig = JsonSerializer.Deserialize<ActionsConfig>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new PromptTypeJsonConverter(), new ParamTypeJsonConverter() }
        })!;


        foreach (var action in scriptConfig.Actions)
        {
            var defaultSet = new ArgumentSet()
            {
                Description = "<default>"
            };
            foreach (var param in action.Params)
            {
                defaultSet.Arguments[param.Name] = param.Default;
            }

            foreach (var set in action.PredefinedArgumentSets.Where(x=>x.FallbackToDefault))
            {
                foreach (var (key,val) in defaultSet.Arguments)
                {
                    if (set.Arguments.ContainsKey(key) == false)
                    {
                        set.Arguments[key] = val;
                    }
                }
            }

            action.PredefinedArgumentSets.Insert(0, defaultSet);
        }

        return scriptConfig.Actions;
    }
}