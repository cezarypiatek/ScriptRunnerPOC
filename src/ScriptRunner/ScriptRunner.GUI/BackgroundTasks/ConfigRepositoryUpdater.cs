using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
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
            Commands.Fetch(repo, remote.Name, refSpecs, null, logMessage);

            if (repo.Head.TrackingDetails.BehindBy > 0)
            {
                // Repo is out of date
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
            FetchOptions = new FetchOptions()
        };

        //options.FetchOptions.CredentialsProvider = new CredentialsHandler(
        //    (url, usernameFromUrl, types) =>
        //        new UsernamePasswordCredentials()
        //        {
        //            Username = USERNAME,
        //            Password = PASSWORD
        //        });

        // User information to create a merge commit
        var signature = repo.Config.BuildSignature(DateTimeOffset.Now);

        var mergeResult = Commands.Pull(repo, signature, options);
        var success = mergeResult.Status != MergeStatus.Conflicts;

        return Task.FromResult(success);
    }
}