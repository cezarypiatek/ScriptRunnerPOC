using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptRunner.GUI;

public class ActionsConfig
{
    public IEnumerable<ScriptConfig> Actions { get; set; }
}

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

public enum PromptType
{
    Text,
    Password,
    Dropdown,
    Multiselect,
    Datepicker,
    Checkbox,
    Multilinetext
}

public enum ParamType
{
    String
}

public class PromptTypeJsonConverter : JsonConverter<PromptType>
{
    public override PromptType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!.ToLowerInvariant() switch
        {
            "text" => PromptType.Text,
            "password" => PromptType.Password,
            _ => PromptType.Text
        };
    }

    public override void Write(Utf8JsonWriter writer, PromptType promptTypeValue, JsonSerializerOptions options)
    {
        writer.WriteStringValue(promptTypeValue.ToString().ToLowerInvariant());
    }
}

public class ParamTypeJsonConverter : JsonConverter<ParamType>
{
    public override ParamType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!.ToLowerInvariant() switch
        {
            "string" => ParamType.String,
            _ => ParamType.String
        };
    }

    public override void Write(Utf8JsonWriter writer, ParamType paramTypeValue, JsonSerializerOptions options)
    {
        writer.WriteStringValue(paramTypeValue.ToString().ToLowerInvariant());
    }
}