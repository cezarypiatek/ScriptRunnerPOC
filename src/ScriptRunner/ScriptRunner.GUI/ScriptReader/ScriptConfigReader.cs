using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
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
    public static ConfigLoadResult LoadWithErrorTracking(ConfigScriptEntry source,
        ScriptRunnerAppSettings appSettings)
    {
        var result = new ConfigLoadResult();
        
        if (string.IsNullOrWhiteSpace(source.Path))
        {
            return result;
        }
        
        if (source.Type == ConfigScriptType.File)
        {
            if (File.Exists(source.Path) == false)
            {
                return result;
            }

            LoadFileSourceWithTracking(source.Path, appSettings, result, source);
            return result;
        }

        if (source.Type == ConfigScriptType.Directory)
        {
            if (Directory.Exists(source.Path) == false)
            {
                return result;
            }

            foreach (var file in Directory.EnumerateFiles(source.Path, "*.json", source.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                LoadFileSourceWithTracking(file, appSettings, result, source);
            }
        }
        
        return result;
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

    private static void LoadFileSourceWithTracking(string fileName, ScriptRunnerAppSettings appSettings, ConfigLoadResult result, ConfigScriptEntry source)
    {
        if (!File.Exists(fileName)) return;

        try
        {
            var jsonString = File.ReadAllText(fileName);

            if (jsonString.Contains("ScriptRunnerSchema.json") == false)
            {
                return;
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
                action.SourceName = source.Name;
                action.Categories ??= new List<string>();
                if (string.IsNullOrWhiteSpace(action.SourceName) == false)
                {
                    action.Categories.Add(action.SourceName);
                }
                else
                {
                    var dir =  Path.GetDirectoryName(source.Path)?.Split(new[]{'\\','/'}).LastOrDefault();
                    if (string.IsNullOrWhiteSpace(dir) == false)
                    {
                        action.Categories.Add(dir);
                    }

                    var alternativeName = Path.GetFileName(source.Path);
                    action.SourceName = string.IsNullOrWhiteSpace(alternativeName) == false ? alternativeName : dir;
                }
                
                
                NormalizeCategories(action.Categories);

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

                [return:NotNullIfNotNull("command")]
                string? AdjustCommandPath(string? command)
                {
                    if (string.IsNullOrWhiteSpace(command) == false && (command.StartsWith(".")))
                    {
                        var (commandPath, args) = MainWindowViewModel.SplitCommandAndArgs(command);
                        return (ResolveAbsolutePath(commandPath) + " " + args).Trim();
                    }

                    return command;
                }
                
                action.Command = AdjustCommandPath(action.Command);
                action.InstallCommand = AdjustCommandPath(action.InstallCommand);
                
                var autoGeneratedParameters = action.Params.Where(x=>x.SkipFromAutoParameterBuilder == false).Select(param => parameterBuilder.Build(param)).Where(paramString => string.IsNullOrWhiteSpace(paramString) == false);
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
                
                action.WorkingDirectory = string.IsNullOrWhiteSpace(action.WorkingDirectory) ? actionDir : ResolveAbsolutePath(action.WorkingDirectory);
                action.InstallCommandWorkingDirectory = string.IsNullOrWhiteSpace(action.InstallCommandWorkingDirectory) ? actionDir : ResolveAbsolutePath(action.InstallCommandWorkingDirectory);

                var defaultSet = new ArgumentSet()
                {
                    Description = MainWindowViewModel.DefaultParameterSetName
                };

                foreach (var param in action.Params)
                {
                    param.ValueGeneratorCommand = AdjustCommandPath(param.ValueGeneratorCommand);
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

               
                
                result.Configs.Add(action);
            }
        }
        catch (Exception ex)
        {
            result.CorruptedFiles.Add(fileName);
        }
    }

    private static readonly Dictionary<string, string> NormalizeCategoriesCache = new();
    private static void NormalizeCategories(List<string> actionCategories)
    {
        var normalize = actionCategories.Select(cat =>
        {
            var key = cat.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
            if (NormalizeCategoriesCache.TryGetValue(key, out var normalized))
            {
                return normalized;
            }
            return NormalizeCategoriesCache[key] = cat.Trim();
            
        }).Distinct().ToList();
        actionCategories.Clear();
        actionCategories.AddRange(normalize);
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

                [return:NotNullIfNotNull("command")]
                string? AdjustCommandPath(string? command)
                {
                    if (string.IsNullOrWhiteSpace(command) == false && (command.StartsWith(".")))
                    {
                        var (commandPath, args) = MainWindowViewModel.SplitCommandAndArgs(command);
                        return (ResolveAbsolutePath(commandPath) + " " + args).Trim();
                    }

                    return command;
                }
                
                action.Command = AdjustCommandPath(action.Command);
                action.InstallCommand = AdjustCommandPath(action.InstallCommand);
                
                var autoGeneratedParameters = action.Params.Where(x=>x.SkipFromAutoParameterBuilder == false).Select(param => parameterBuilder.Build(param)).Where(paramString => string.IsNullOrWhiteSpace(paramString) == false);
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
                
                action.WorkingDirectory = string.IsNullOrWhiteSpace(action.WorkingDirectory) ? actionDir : ResolveAbsolutePath(action.WorkingDirectory);
                action.InstallCommandWorkingDirectory = string.IsNullOrWhiteSpace(action.InstallCommandWorkingDirectory) ? actionDir : ResolveAbsolutePath(action.InstallCommandWorkingDirectory);

                var defaultSet = new ArgumentSet()
                {
                    Description = MainWindowViewModel.DefaultParameterSetName
                };

                foreach (var param in action.Params)
                {
                    param.ValueGeneratorCommand = AdjustCommandPath(param.ValueGeneratorCommand);
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

                // Format InstallCommand if it exists
                if (!string.IsNullOrWhiteSpace(action.InstallCommand))
                {
                    var installWithMarkers = action.Params.Aggregate
                    (
                        seed: action.InstallCommand,
                        func: (string accumulate, ScriptParam source) =>
                            accumulate.Replace("{" + source.Name + "}", "[!@#]{" + source.Name + "}[!@#]")
                    );

                    action.InstallCommandFormatted.AddRange(installWithMarkers.Split("[!@#]").Select(x =>
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
            }

            return scriptConfig.Actions;
        }
        catch
        {
            return Enumerable.Empty<ScriptConfig>();
        }
    }
}