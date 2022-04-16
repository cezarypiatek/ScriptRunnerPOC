using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

public class ParamTypeJsonConverter : JsonConverter<ParamType>
{
    public override ParamType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!.ToLowerInvariant() switch
        {
            "string" => ParamType.String,
            "bool" => ParamType.Bool,
            "number" => ParamType.Number,
            _ => ParamType.String
        };
    }

    public override void Write(Utf8JsonWriter writer, ParamType paramTypeValue, JsonSerializerOptions options)
    {
        writer.WriteStringValue(paramTypeValue.ToString().ToLowerInvariant());
    }
}