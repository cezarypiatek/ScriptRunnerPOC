using System;
using System.IO;
using System.Text.Json;

namespace ScriptRunner.GUI;

public static class ScriptConfigReader
{
    public static ActionsConfig Load()
    {
        var fileName = Path.Combine(AppContext.BaseDirectory,"Scripts/TextInputScript.json");
        var jsonString = File.ReadAllText(fileName);
        var scriptConfig = JsonSerializer.Deserialize<ActionsConfig>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new PromptTypeJsonConverter(), new ParamTypeJsonConverter() }
        })!;

        return scriptConfig;
    }
}