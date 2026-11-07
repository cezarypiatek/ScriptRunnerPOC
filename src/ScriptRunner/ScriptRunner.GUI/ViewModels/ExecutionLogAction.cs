using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DynamicData;
using ScriptRunner.GUI.Infrastructure;

namespace ScriptRunner.GUI.ViewModels;

public record ExecutionLogAction(DateTime Timestamp, string? Source, string Name, Dictionary<string, string> Parameters)
{
    [JsonIgnore]
    public InlineCollection ParametersDescription => new InlineCollection()
    {
        new Run("["),
        
        Parameters.SelectMany((x,i) =>
        {
            var value = VaultReference.TryParse(x.Value, out _) ? "*****" : x.Value;
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
    
    [JsonIgnore]
    public IEnumerable<ParameterTag> ParameterTags => Parameters
        .OrderBy(x => x.Key)
        .Select(x => new ParameterTag(
            x.Key, 
            x.Value ?? string.Empty, 
            isMasked: VaultReference.TryParse(x.Value, out _)));
    
    public string ParametersDescriptionString() => string.Join(", ", Parameters.OrderBy(x=>x.Key).Select(x => $"{x.Key} = {x.Value}"));
};
