using System.CommandLine;
using System.CommandLine.IO;
using System.Diagnostics;
using System.IO.Compression;

internal class Program
{

    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("AppInstaller CLI");

        rootCommand.AddCommand(CreateUpdateDotnetToolCommand());
        rootCommand.AddCommand(CreateDownloadZipCommand());
        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Command unknown");
        });
        try
        {
            var result = await rootCommand.InvokeAsync(args, new SystemConsole());
            if (result != 0)
            {
                Console.WriteLine("Press key to continue...");
                Console.ReadKey();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.ReadKey();
        }
    }

    private static Command CreateUpdateDotnetToolCommand()
    {
        var updateDotnetToolCommand = new Command("dotnet-tool", "Update dotnet tool");
        var packageNameOption = new Option<string>("--packageName") {IsRequired = true};
        updateDotnetToolCommand.AddOption(packageNameOption);
        var versionOption = new Option<string>("--version") {IsRequired = false};
        updateDotnetToolCommand.AddOption(versionOption);
        updateDotnetToolCommand.SetHandler((packageName, version) =>
        {
            Console.WriteLine($"Updating dotnet tool {packageName}");
            var command = string.IsNullOrWhiteSpace(version) == false 
                ? $"tool update {packageName} --global --no-cache --ignore-failed-sources --version {version} --add-source https://api.nuget.org/v3/index.json"
                : $"tool update {packageName} --global --no-cache --ignore-failed-sources --add-source https://api.nuget.org/v3/index.json";
            var process = Process.Start("dotnet", command);
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Press key to continue...");
                Console.ReadKey();
            }
            
            Process.Start(packageName);
        }, packageNameOption, versionOption);
        return updateDotnetToolCommand;
    }
    
    private static Command CreateDownloadZipCommand()
    {
        var downloadZipCommand = new Command("download-zip", "Download ZIP");
        var startingProcessOption = new Option<string>("--startingProcess") {IsRequired = true};
        downloadZipCommand.AddOption(startingProcessOption);
        var downloadPathOption = new Option<string>("--downloadPath") {IsRequired = true};
        downloadZipCommand.AddOption(downloadPathOption);
        downloadZipCommand.SetHandler(async (startingProcess, downloadPath) =>
        {
            try
            {
                Console.WriteLine($"Download package from {downloadPath}");
                using var wc = new HttpClient();
                var stream = await wc.GetStreamAsync(downloadPath);
                var destination = Path.GetDirectoryName(startingProcess);
                Console.WriteLine($"Unpacking to {destination}");
                var archive = new ZipArchive(stream);
                try
                {
                    Directory.Delete(destination, recursive: true);
                    Directory.CreateDirectory(destination);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                archive.ExtractToDirectory(destination);
            }
            catch (Exception ex)
            {
                Debugger.Launch();
                Console.WriteLine(ex);
            }
            finally
            {
                Process.Start(startingProcess);
            }
        }, startingProcessOption, downloadPathOption);
        return downloadZipCommand;
    }
}