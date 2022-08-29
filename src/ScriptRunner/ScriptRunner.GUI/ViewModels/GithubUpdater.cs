using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScriptRunner.GUI.ViewModels;

public class GithubUpdater
{
    public string GetProductFullVersion()
    {
        var assemblyVersion = this.GetType().Assembly.GetName().Version;
        return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
    }

    public class ReleaseResponse
    {
        public string tag_name { get; set; }
        public List<GithubReleaseAsset> assets { get; set; }
    }

    public class GithubReleaseAsset
    {
        public string browser_download_url { get; set; }
        public string name { get; set; }
    }

    string? LatestVersionDownloadLink { get; set; }
    public async Task<bool> CheckIsNewerVersionAvailable()
    {
        try
        {
            using var httpClient = new HttpClient();
            var currentProductVersionRaw = GetProductFullVersion();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ScriptRunner", currentProductVersionRaw));
            var response = await httpClient.GetAsync("https://api.github.com/repos/cezarypiatek/ScriptRunnerPOC/releases/latest").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReleaseResponse>(payload);
                LatestVersionDownloadLink = result?.assets?.FirstOrDefault()?.browser_download_url;
                if (string.IsNullOrWhiteSpace(result?.tag_name) == false)
                {
                    var latestVersion = Version.Parse(result.tag_name);

                    var currentVersion = Version.Parse(currentProductVersionRaw);
                    return latestVersion > currentVersion;
                }
            }
        }
        catch (Exception e)
        {
            return false;
        }

        return false;
    }

    public void OpenLatestReleaseLog()
    {
        OpenWebsite(@"https://github.com/cezarypiatek/ScriptRunnerPOC/releases/");
    }

    private static void OpenWebsite(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url.Replace("&", "^&"),
            UseShellExecute = true,
            CreateNoWindow = false
        });
    }

    public void InstallLatestVersion()
    {
        if (string.IsNullOrWhiteSpace(LatestVersionDownloadLink) == false)
        {
            var installerPath = Path.GetTempPath();
            Directory.CreateDirectory(installerPath);
            ExtractArchiveFile(this.GetType().Assembly, "AppInstaller.zip", installerPath);
            Process.Start(new ProcessStartInfo(Path.Combine(installerPath, "AppInstaller\\AppInstaller.exe"))
            {
                ArgumentList =
                {
                    Process.GetCurrentProcess().MainModule?.FileName ?? "",
                    LatestVersionDownloadLink
                },
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });
            Process.GetCurrentProcess().Kill();
        }
        
    }

    public static void ExtractArchiveFile(Assembly assembly, string resourceName, string installationPath)
    {
        var assemblyNamePrefix = assembly.GetName().Name;
        using var stream = assembly.GetManifestResourceStream($"{assemblyNamePrefix}.{resourceName}");
        if (stream == null) return;
        using var archive = new ZipArchive(stream);
        
        var archiveFileName = resourceName.Replace($"{assemblyNamePrefix}.", "");
        var archiveDestinationDirectory = Path.Combine(installationPath, archiveFileName.Replace(".zip", ""));
        archive.ExtractToDirectory(archiveDestinationDirectory, overwriteFiles: true);
    }
}