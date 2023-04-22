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
            var installerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScriptRunnerUpdater");
            if (Directory.Exists(installerPath))
            {
                Directory.Delete(installerPath, true);
            }
            Directory.CreateDirectory(installerPath);
            ExtractArchiveFile(this.GetType().Assembly, "AppInstaller.zip", installerPath);
            
            var currentProcess = Process.GetCurrentProcess();
            if (currentProcess.ProcessName == "scriptrunnergui")
            {
                Process.Start(new ProcessStartInfo("dotnet")
                {
                    WorkingDirectory = Path.Combine(installerPath, "AppInstaller"),
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = "AppInstaller.dll dotnet-tool --packageName scriptrunnergui"
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo("dotnet")
                {
                    WorkingDirectory = Path.Combine(installerPath, "AppInstaller"),
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = $"AppInstaller.dll download-zip --startingProcess \"{currentProcess.MainModule?.FileName}\" --downloadPath \"{LatestVersionDownloadLink}\"",

                });
            }
            
            currentProcess.Kill();
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