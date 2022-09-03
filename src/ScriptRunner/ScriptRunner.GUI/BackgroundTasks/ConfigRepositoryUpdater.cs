using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Alm.Authentication;
using ScriptRunner.GUI.Settings;

namespace ScriptRunner.GUI.BackgroundTasks;

public static class ConfigRepositoryUpdater
{
    public static Task<List<OutdatedRepositoryModel>> CheckAllRepositories()
    {
        var outOfDateRepos = new List<OutdatedRepositoryModel>();
        var entries = AppSettingsService.Load().ConfigScripts?.Where(e => e.Type == ConfigScriptType.Directory) ??
                      Array.Empty<ConfigScriptEntry>();

        foreach (var entry in entries)
        {
            if (!Repository.IsValid(entry.Path)) continue;

            var logMessage = string.Empty;
            using var repo = new Repository(entry.Path);
            var remote = repo.Network.Remotes["origin"];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

            var options = new FetchOptions
            {
                CredentialsProvider = CreateCredentialsHandler()
            };

            Commands.Fetch(repo, remote.Name, refSpecs, options, logMessage);

            if (repo.Head.TrackingDetails.BehindBy > 0)
            {
                outOfDateRepos.Add(new OutdatedRepositoryModel(entry.Path));
            }
        }

        return Task.FromResult(outOfDateRepos);
    }

    public static Task<bool> PullRepository(string path)
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
        var secrets = new SecretStore("git");
        var auth = new BasicAuthentication(secrets);

        return new CredentialsHandler((repoUrl, usernameFromUrl, types) =>
        {
            var url = new Uri(repoUrl);
            var credentials = auth.GetCredentials(new TargetUri(url.GetLeftPart(UriPartial.Authority)));

            //TODO: if credentials not found - what to do? Prompt/ignore/show message?

            return new UsernamePasswordCredentials
            {
                Username = credentials.Username,
                Password = credentials.Password
            };
        });
    }
}