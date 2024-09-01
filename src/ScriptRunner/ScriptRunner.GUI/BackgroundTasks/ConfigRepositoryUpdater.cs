using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using ScriptRunner.GUI.Settings;

namespace ScriptRunner.GUI.BackgroundTasks;

public interface IRepositoryClient
{
    Task<bool> IsOutdated(string repoPath);
    Task<bool> PullRepository(string path);
}

public record CliCommand(string Command, string Parameters, string WorkingDirectory);

public delegate Task<CliCommandOutputs> CliCommandExecutor(CliCommand command);

public record CliCommandOutputs(string StandardOutput, string StandardError);
class CliRepositoryClient : IRepositoryClient
{
    private readonly CliCommandExecutor _cliCommandExecutor;

    public CliRepositoryClient(CliCommandExecutor cliCommandExecutor)
    {
        _cliCommandExecutor = cliCommandExecutor;
    }

    public async Task<bool> IsOutdated(string repoPath)
    {
        _ = await ExecuteCommand(repoPath, "git", "fetch --prune origin --verbose");
        var (success, result) = await ExecuteCommand(repoPath, "git", "status -uno");
        return success && result.Contains("up to date", StringComparison.InvariantCultureIgnoreCase) == false;
    }

    public async Task<bool> PullRepository(string path)
    {
        var result = await _cliCommandExecutor.Invoke(new CliCommand("git", "pull --rebase=true origin --prune --verbose", path));
        return result.StandardError.Contains("error", StringComparison.InvariantCultureIgnoreCase) == false;
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



public class ConfigRepositoryUpdater
{
    private readonly IRepositoryClient repositoryClient;


    public ConfigRepositoryUpdater(IRepositoryClient repositoryClient)
    {
        this.repositoryClient = repositoryClient;
    }

    public async Task<List<OutdatedRepositoryModel>> CheckAllRepositories()
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

   

    public Task<bool> RefreshRepository(string path)
    {
        return repositoryClient.PullRepository(path);
    }
}