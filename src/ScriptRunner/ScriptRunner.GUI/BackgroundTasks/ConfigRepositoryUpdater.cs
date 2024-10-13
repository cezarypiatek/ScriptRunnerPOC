using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using ScriptRunner.GUI.Settings;

namespace ScriptRunner.GUI.BackgroundTasks;

public interface IRepositoryClient
{
    Task<(bool,string)> IsOutdated(string repoPath);
    Task<(bool, IReadOnlyList<string>)> PullRepository(string path);
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

    public async Task<(bool,string)> IsOutdated(string repoPath)
    {
        _ = await ExecuteCommand(repoPath, "git", "fetch --prune origin --verbose");
       if(await GetHeadBranchName(repoPath) is {} mainBranch)
        {
            var isMainBranchOutdated = await IsBranchOutdated(repoPath, mainBranch, mainBranch);
            if (isMainBranchOutdated)
            {
                if(await GetCurrentBranchName(repoPath) is {} currentBranch  && string.IsNullOrWhiteSpace(currentBranch) == false && currentBranch != mainBranch)
                {
                    var isCurrentBranchOutdated = await IsBranchOutdated(repoPath, currentBranch, mainBranch);
                    if (isCurrentBranchOutdated == false)
                    {
                        return (false, mainBranch);
                    }
                }
                
            }
            return (isMainBranchOutdated, mainBranch);
        }
        
        var (success, result) = await ExecuteCommand(repoPath, "git", "status -uno");
        var isOutdated = success && result.Contains("up to date", StringComparison.InvariantCultureIgnoreCase) == false;
        return (isOutdated, "current");
    }

    private static async Task<bool> IsBranchOutdated(string repoPath, string sourceBranch, string targetBranch)
    {
        var (_, statusForBranch) = await ExecuteCommand(repoPath, "git", $"log {sourceBranch}..origin/{targetBranch} --oneline");
        var outdated = string.IsNullOrWhiteSpace(statusForBranch) == false;
        return outdated;
    }

    static async Task<string?> GetHeadBranchName(string repoPath)
    {
        var (_, originDetectOutput) = await ExecuteCommand(repoPath, "git", "remote show origin");

        // Use regular expression to match the 'HEAD branch' line
        var match = Regex.Match(originDetectOutput, @"HEAD branch:\s*(\S+)");
        if (match.Success)
        {
            return match.Groups[1].Value?.Trim(); // Return the branch name captured by the group
        }

        return null;
    }
    static async Task<string?> GetCurrentBranchName(string repoPath)
    {
        var (_, originDetectOutput) = await ExecuteCommand(repoPath, "git", "rev-parse --abbrev-ref HEAD");
        return originDetectOutput?.Trim();
    }
    
    public async Task<(bool, IReadOnlyList<string>)>  PullRepository(string path)
    {
        _ = await ExecuteCommand(path, "git", "fetch --prune origin --verbose");

        var mainBranch = await GetHeadBranchName(path) ?? "";
        var (_, log) = await ExecuteCommand(path, "git", $"log {mainBranch}..origin/{mainBranch} --pretty=format:\"%s\"");
        var result = await _cliCommandExecutor.Invoke(new CliCommand("git", $"rebase origin/{mainBranch} {mainBranch} --verbose", path));
        var pulledWithSuccess = (result.StandardError.Contains("error", StringComparison.InvariantCultureIgnoreCase)  ||
                                result.StandardError.Contains("fatal", StringComparison.InvariantCultureIgnoreCase)) == false;
        return (pulledWithSuccess, log?.Split('\n')??Array.Empty<string>());
    }

    private static async Task<(bool, string)> ExecuteCommand(string repoPath, string command, string parameters)
    {
        var sb = new StringBuilder();
        try
        {
            await Cli.Wrap(command)
                .WithArguments(parameters)
                .WithWorkingDirectory(repoPath)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(s => { sb.AppendLine(s); }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(s => { sb.AppendLine(s); }))
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

            var (isOutdated, branchName) = await repositoryClient.IsOutdated(entry.Path);

            if (isOutdated)
            {
                outOfDateRepos.Add(new OutdatedRepositoryModel()
                {
                    Path = entry.Path,
                    BranchName = branchName
                });
            }
        }

        return outOfDateRepos;
    }

   

    public Task<(bool, IReadOnlyList<string>)> RefreshRepository(string path)
    {
        return repositoryClient.PullRepository(path);
    }
}