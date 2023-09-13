using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Remote.Protocol.Viewport;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.ScriptReader;

public static class ScriptConfigReader
{
    public static IEnumerable<ScriptConfig> Load(ConfigScriptEntry source,
        ScriptRunnerAppSettings appSettings)
    {
        if (string.IsNullOrWhiteSpace(source.Path))
        {
            yield break;
        }
        
        if (source.Type == ConfigScriptType.File)
        {
            if (File.Exists(source.Path) == false)
            {
                yield break;
            }

            foreach (var scriptConfig in LoadFileSource(source.Path, appSettings))
            {
                scriptConfig.SourceName = source.Name;
                scriptConfig.Categories ??= new List<string>();

                var mainCategory = string.IsNullOrWhiteSpace(scriptConfig.SourceName) == false
                    ? scriptConfig.SourceName
                    : Path.GetFileName(source.Path);
                if (string.IsNullOrWhiteSpace(mainCategory) == false)
                {
                    scriptConfig.Categories.Add(mainCategory);

                    if (mainCategory != source.Name)
                    {
                        source.Name = mainCategory;
                    }
                }
                yield return scriptConfig;
            }
            yield break;
        }

        if (source.Type == ConfigScriptType.Directory)
        {
            if (Directory.Exists(source.Path) == false)
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(source.Path, "*.json", source.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                foreach (var scriptConfig in LoadFileSource(file, appSettings))
                {
                    scriptConfig.SourceName = source.Name;
                    scriptConfig.Categories ??= new List<string>();

                    var mainCategory = string.IsNullOrWhiteSpace(scriptConfig.SourceName) == false
                        ? scriptConfig.SourceName
                        :   Path.GetFileName(source.Path);
                    if (string.IsNullOrWhiteSpace(mainCategory) == false)
                    {
                        scriptConfig.Categories.Add(mainCategory);

                        if (mainCategory != source.Name)
                        {
                            source.Name = mainCategory;
                        }
                    }
                    yield return scriptConfig;
                }
            }
        }
    }

    private static IAutoParameterBuilder CreateBuilder(ScriptConfig scriptConfig)
    {
        if (scriptConfig.AutoParameterBuilderStyle == "powershell")
        {
            return PowerShellAutoParameterBuilder.Instance;
        }

        if (string.IsNullOrWhiteSpace(scriptConfig.AutoParameterBuilderPattern) == false)
        {
            return new PatternBaseBuilderAutoParameterBuilder(scriptConfig.AutoParameterBuilderPattern);
        }

        return EmptyAutoParameterBuilder.Instance;
    }

    private static IEnumerable<ScriptConfig> LoadFileSource(string fileName,
        ScriptRunnerAppSettings appSettings)
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
                AllowTrailingCommas = true,
                Converters = { new PromptTypeJsonConverter(), new ParamTypeJsonConverter(), new JsonStringEnumConverter() }
            })!;

            foreach (var action in scriptConfig.Actions)
            {
                action.Source = fileName;
                var parameterBuilder = CreateBuilder(action);

                var actionDir = Path.GetDirectoryName(fileName);
                
                string ResolveAbsolutePath(string path)
                {
                    if (string.IsNullOrWhiteSpace(path) == false)
                    {
                        if (Path.IsPathRooted(path) == false)
                        {
                            return Path.GetFullPath(Path.Combine(actionDir!, path));
                        }
                    }

                    return path;
                }

                if (string.IsNullOrWhiteSpace(action.Command) == false && (action.Command.StartsWith(".")))
                {
                    var (commandPath, args) = MainWindowViewModel.SplitCommandAndArgs(action.Command);
                    action.Command = (ResolveAbsolutePath(commandPath) + " " + args).Trim();
                }
                
                if (string.IsNullOrWhiteSpace(action.InstallCommand) == false && (action.Command.StartsWith(".")))
                {
                    var (commandPath, args) = MainWindowViewModel.SplitCommandAndArgs(action.InstallCommand);
                    action.InstallCommand = (ResolveAbsolutePath(commandPath) + " " + args).Trim();
                }
                
                var autoGeneratedParameters = action.Params.Select(param => parameterBuilder.Build(param)).Where(paramString => string.IsNullOrWhiteSpace(paramString) == false);
                action.Command += " "+string.Join(" ", autoGeneratedParameters);

                if (string.IsNullOrWhiteSpace(action.Docs) == false )
                {
                    var docPaths = ResolveAbsolutePath(action.Docs);
                    if (File.Exists(docPaths))
                    {
                        action.HasDocs = true;
                        action.DocsContent = File.ReadAllText(docPaths);
                        action.DocAssetPath = Path.GetDirectoryName(docPaths);
                    }
                }
                
                if (string.IsNullOrWhiteSpace(action.WorkingDirectory))
                {
                    action.WorkingDirectory = actionDir;
                }
                else
                {
                    action.WorkingDirectory = ResolveAbsolutePath(action.WorkingDirectory);
                }

                if (string.IsNullOrWhiteSpace(action.InstallCommandWorkingDirectory))
                {
                    action.InstallCommandWorkingDirectory = actionDir;
                }
                else
                {
                    action.InstallCommandWorkingDirectory = ResolveAbsolutePath(action.InstallCommandWorkingDirectory);
                }

                var defaultSet = new ArgumentSet()
                {
                    Description = MainWindowViewModel.DefaultParameterSetName
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

                if (appSettings.ExtraParameterSets?.Where(x => x.ActionName == action.Name).ToList() is { } extraSets)
                {
                    foreach (var extraSet in extraSets.Select(x => new ArgumentSet
                             {
                                 Description = x.Description,
                                 Arguments = x.Arguments
                             }))
                    {
                        var existing = action.PredefinedArgumentSets.FirstOrDefault(x => x.Description == extraSet.Description);
                        if (existing != null)
                        {
                            action.PredefinedArgumentSets[action.PredefinedArgumentSets.IndexOf(existing)] = extraSet;
                        }
                        else
                        {
                            action.PredefinedArgumentSets.Add(extraSet);
                        }
                    }

                    
                }

                switch (action.PredefinedArgumentSetsOrdering)
                {
                    case PredefinedArgumentSetsOrdering.Ascending:
                        action.PredefinedArgumentSets.Sort((s1, s2) => string.CompareOrdinal(s1.Description, s2.Description));
                        break;
                    case PredefinedArgumentSetsOrdering.Descending:
                        action.PredefinedArgumentSets.Sort((s1, s2) => string.CompareOrdinal(s2.Description, s1.Description));
                        break;
                }

                action.PredefinedArgumentSets.Insert(0, defaultSet);

                foreach (var param in action.Params.Where(x=>x.Prompt == PromptType.FileContent))
                {
                    foreach (var set in action.PredefinedArgumentSets)
                    {
                        if (set.Arguments.TryGetValue(param.Name, out var defaultValue))
                        {

                           

                            set.Arguments[param.Name] = ResolveAbsolutePath(defaultValue);
                        }
                    }
                }

                var withMarkers = action.Params.Aggregate
                (
                    seed: action.Command,
                    func: (string accumulate, ScriptParam source) =>
                        accumulate.Replace("{" + source.Name + "}", "[!@#]{" + source.Name + "}[!@#]")
                );

                action.CommandFormatted.AddRange(withMarkers.Split("[!@#]").Select(x =>
                {
                    var inline = new Run(x);
                    if (x.StartsWith("{"))
                    {

                        inline.Foreground = Brushes.LightGreen;
                        inline.FontWeight = FontWeight.ExtraBold;
                    }

                    return inline;
                }));
            }

            return scriptConfig.Actions;
        }
        catch
        {
            return Enumerable.Empty<ScriptConfig>();
        }
    }
}