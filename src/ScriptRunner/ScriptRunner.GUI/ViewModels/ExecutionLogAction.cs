using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DynamicData;

namespace ScriptRunner.GUI.ViewModels;

public record ExecutionLogAction(DateTime Timestamp, string Source, string Name, Dictionary<string, string> Parameters)
{
    [JsonIgnore]
    public InlineCollection ParametersDescription => new InlineCollection()
    {
        new Run("["),
        
        Parameters.SelectMany((x,i) =>
        {
            var value = x.Value?.StartsWith("!!vault:") == true ? "*****" : x.Value;
            return new[]
            {
                new Run($"{x.Key} = "),
                new Run(value)
                {
                    Foreground = Brushes.LightGreen,
                },
                new Run( i< Parameters.Count -1?", ":"")
            };
        }),
        new Run("]"),
    };
    
    public string ParametersDescriptionString() => string.Join(", ", Parameters.OrderBy(x=>x.Key).Select(x => $"{x.Key} = {x.Value}"));
};