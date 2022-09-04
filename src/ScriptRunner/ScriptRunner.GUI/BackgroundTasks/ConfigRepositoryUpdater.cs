using System;
using System.Collections.Generic;
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

public class LibGit2SharpRepositoryClient : IRepositoryClient
{
    public Task<bool> IsOutdated(string repoPath)
    {
        var logMessage = string.Empty;
        using var repo = new Repository(repoPath);
        var remote = repo.Network.Remotes["origin"];
        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

        var options = new FetchOptions
        {
            CredentialsProvider = CreateCredentialsHandler()
        };

        Commands.Fetch(repo, remote.Name, refSpecs, options, logMessage);
        var isOutdated = repo.Head.TrackingDetails.BehindBy > 0;
        return Task.FromResult(isOutdated);
    }

    public Task<bool> PullRepository(string path)
    {
        using var repo = new Repository(path);

        var options = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                CredentialsProvider = CreateCredentialsHandler()
            }
        };

        var signature = repo.Config.BuildSignature(DateTimeOffset.Now);

        var mergeResult = Commands.Pull(repo, signature, options);
        var success = mergeResult.Status != MergeStatus.Conflicts;

        return Task.FromResult(success);
    }

    private static CredentialsHandler CreateCredentialsHandler()
    {
        throw new NotImplementedException();
        //var secrets = new SecretStore("git");
        //var auth = new BasicAuthentication(secrets);

        //return new CredentialsHandler((repoUrl, usernameFromUrl, types) =>
        //{
        //    var url = new Uri(repoUrl);
        //    var credentials = auth.GetCredentials(new TargetUri(url.GetLeftPart(UriPartial.Authority)));

        //    //TODO: if credentials not found - what to do? Prompt/ignore/show message?

        //    return new UsernamePasswordCredentials
        //    {
        //        Username = credentials.Username,
        //        Password = credentials.Password
        //    };
        //});
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
            if (!Repository.IsValid(entry.Path)) continue;

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