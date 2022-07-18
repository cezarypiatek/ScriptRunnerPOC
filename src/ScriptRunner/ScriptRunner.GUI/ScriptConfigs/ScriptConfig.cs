﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace ScriptRunner.GUI.ScriptConfigs;

public class ScriptConfig
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Command { get; set; }
    public string? InstallCommand { get; set; }
    public string? InstallCommandWorkingDirectory { get; set; }
    public string? WorkingDirectory { get; set; }
    public List<ScriptParam> Params { get; set; } = new();
    public List<ArgumentSet> PredefinedArgumentSets { get; set; } = new();
    public Dictionary<string, string?> EnvironmentVariables { get; set; } = new();
    public string? Source { get; set; }

}
public class ArgumentSet
{
    public string Description { get; set; }
    public bool FallbackToDefault { get; set; }
    public Dictionary<string, string> Arguments { get; set; } = new();
}
public class ScriptParam
{
    public string Name { get; set; }
    public string Description { get; set; }
    public PromptType Prompt { get; set; }
    public string Default { get; set; }
    public Dictionary<string, string> PromptSettings { get; set; } = new();

    public bool GetPromptSettings(string name, [NotNullWhen(true)] out string? value)
    {
        return PromptSettings.TryGetValue(name, out value);
    }
    
    public T GetPromptSettings<T>(string name, Func<string,T> convert, T @default)
    {
        if (PromptSettings.TryGetValue(name, out var value))
        {
            return convert(value);
        }

        return @default;
    }

}