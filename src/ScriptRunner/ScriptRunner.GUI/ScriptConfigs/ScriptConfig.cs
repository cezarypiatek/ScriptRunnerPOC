using Avalonia.Controls.Documents;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ScriptRunner.GUI.ViewModels;


namespace ScriptRunner.GUI.ScriptConfigs;

public class ScriptConfig
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Command { get; set; }
    public bool RunCommandAsAdmin { get; set; }
    public string Docs { get; set; }
    public string DocsContent { get; set; } = string.Empty;
    public string DocAssetPath { get; set; }
    public bool HasDocs { get; set; }
    public List<string> Categories { get; set; } = new();
    public string? InstallCommand { get; set; }
    public string? InstallCommandWorkingDirectory { get; set; }
    public bool RunInstallCommandAsAdmin { get; set; }
    public string? WorkingDirectory { get; set; }
    public List<ScriptParam> Params { get; set; } = new();
    public PredefinedArgumentSetsOrdering? PredefinedArgumentSetsOrdering { get; set; }
    public List<ArgumentSet> PredefinedArgumentSets { get; set; } = new();
    public Dictionary<string, string?> EnvironmentVariables { get; set; } = new();
    public string? Source { get; set; }
    public string? SourceName { get; set; }
    public string FullName => string.IsNullOrWhiteSpace(SourceName) ? Name : $"{SourceName} - {Name}";
    public string? AutoParameterBuilderPattern { get; set; }
    public string? AutoParameterBuilderStyle { get; set; }
    public List<InteractiveInputDescription> InteractiveInputs { get; set; } = new();
    public List<TroubleshootingItem> Troubleshooting { get; set; } = new();
    public List<TroubleshootingItem> InstallTroubleshooting { get; set; } = new();
    public InlineCollection CommandFormatted { get; set; } = new();
}

public class TroubleshootingItem
{
    public string WhenMatched { get; set; }
    public string AlertMessage { get; set; }
    public TroubleShootingSeverity Severity { get; set; }
}

public class ArgumentSet
{
    public string Description { get; set; }
    public bool FallbackToDefault { get; set; }
    public Dictionary<string, string> Arguments { get; set; } = new();
}

public enum PredefinedArgumentSetsOrdering
{
    Ascending,
    Descending
}

public class ScriptParam
{
    public string Name { get; set; }
    public string Description { get; set; }
    public PromptType Prompt { get; set; }
    public string Default { get; set; }
    public Dictionary<string, object> PromptSettings { get; set; } = new();
    public string? AutoParameterBuilderPattern { get; set; }
    public string? ValueGeneratorCommand { get; set; }
    public string? ValueGeneratorLabel { get; set; }
    public bool SkipFromAutoParameterBuilder { get; set; }

    public bool GetPromptSettings(string name, [NotNullWhen(true)] out string? value)
    {
        var res = PromptSettings.TryGetValue(name, out var objV);
        value = objV?.ToString();
        return res;
    }
    
    public T GetPromptSettings<T>(string name, Func<string,T> convert, T @default)
    {
        if (PromptSettings.TryGetValue(name, out var value))
        {
            return convert(value.ToString()!);
        }

        return @default;
    }
}

public class InteractiveInputDescription
{
    public string WhenMatched { get; set; }
    public List<InteractiveInputItem> Inputs { get; set; } = new();
}

public class InteractiveInputItem
{
    public string Label { get; set; }
    public string Value { get; set; }
}