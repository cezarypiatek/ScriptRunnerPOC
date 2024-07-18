using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using ScriptRunner.GUI.Settings;

namespace ScriptRunner.GUI.BackgroundTasks;

public interface IRepositoryClient
{
    Task<bool> IsOutdated(string repoPath);
    Task<bool> PullRepository(string path);
}

class CliRepositoryClient : IRepositoryClient
{
    public async Task<bool> IsOutdated(string repoPath)
    {
        _ = await ExecuteCommand(repoPath, "git", "fetch --prune origin --verbose");
        var (success, result) = await ExecuteCommand(repoPath, "git", "status -uno");
        return success && result.Contains("up to date", StringComparison.InvariantCultureIgnoreCase) == false;
    }

    public async Task<bool> PullRepository(string path)
    {
        var (success,_) = await ExecuteCommand(path, "git", "pull --rebase=true origin --prune --verbose");
        return success;
    }

    private static async Task<(bool, string)> ExecuteCommand(string repoPath, string command, string parameters)
    {
        var sb = new StringBuilder();
        try
        {
            await Cli.Wrap(command)
                .WithArguments(parameters)
                .WithWorkingDirectory(repoPath)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(s => { sb.Append(s); }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(s => { sb.Append(s); }))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(default);
        }
        catch (Exception e)
        {
            sb.Append(e);
            return (false, sb.ToString());
        }
        return (true, sb.ToString());
    }
}



public static class ConfigRepositoryUpdater
{
    private static readonly IRepositoryClient repositoryClient = new CliRepositoryClient();

    public static async Task<List<OutdatedRepositoryModel>> CheckAllRepositories()
    {
        var outOfDateRepos = new List<OutdatedRepositoryModel>();
        var entries = AppSettingsService.Load().ConfigScripts?.Where(e => e.Type == ConfigScriptType.Directory) ??
                      Array.Empty<ConfigScriptEntry>();

        foreach (var entry in entries)
        {

            if (Directory.Exists(Path.Combine(entry.Path, ".git")) == false)
            {
                continue;
            }

            var isOutdated = await repositoryClient.IsOutdated(entry.Path);

            if (isOutdated)
            {
                outOfDateRepos.Add(new OutdatedRepositoryModel(entry.Path));
            }
        }

        return outOfDateRepos;
    }

   

    public static Task<bool> PullRepository(string path)
    {
        return repositoryClient.PullRepository(path);
    }

  
}