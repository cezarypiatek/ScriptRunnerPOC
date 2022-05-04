using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ScriptReader;

public class PromptTypeJsonConverter : JsonConverter<PromptType>
{
    public override PromptType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!.ToLowerInvariant() switch
        {
            "text" => PromptType.Text,
            "password" => PromptType.Password,
            "dropdown" => PromptType.Dropdown,
            "datepicker" => PromptType.Datepicker,
            "checkbox" => PromptType.Checkbox,
            "multilinetext" => PromptType.Multilinetext,
            "filepicker" => PromptType.FilePicker,
            "directorypicker" => PromptType.DirectoryPicker,
            _ => PromptType.Text
        };
    }

    public override void Write(Utf8JsonWriter writer, PromptType promptTypeValue, JsonSerializerOptions options)
    {
        writer.WriteStringValue(promptTypeValue.ToString().ToLowerInvariant());
    }
}