using System.CommandLine;
using System.CommandLine.IO;
using System.Diagnostics;
using System.IO.Compression;

internal class Program
{
    private static readonly TimeSpan ParentProcessExitTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ToolUpdateTimeout = TimeSpan.FromMinutes(5);

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
        var processIdOption = new Option<int>("--processId") {IsRequired = true};
        updateDotnetToolCommand.AddOption(processIdOption);
        updateDotnetToolCommand.SetHandler(async (packageName, version, processId) =>
        {
            if (await WaitForProcessExitAsync(processId, ParentProcessExitTimeout) == false)
            {
                Console.Error.WriteLine($"Application process {processId} did not exit within {ParentProcessExitTimeout.TotalSeconds:N0} seconds. Update cancelled.");
                return;
            }

            Console.WriteLine($"Updating dotnet tool {packageName}");
            var command = string.IsNullOrWhiteSpace(version) == false 
                ? $"tool update {packageName} --global --no-cache --ignore-failed-sources --version {version} --verbosity diag --configfile nuget.config"
                : $"tool update {packageName} --global --no-cache --ignore-failed-sources --verbosity diag --configfile nuget.config";
            using var process = Process.Start("dotnet", command)
                ?? throw new InvalidOperationException("Failed to start the dotnet tool updater.");

            if (await WaitForProcessExitAsync(process, ToolUpdateTimeout) == false)
            {
                Console.Error.WriteLine($"The dotnet tool update did not finish within {ToolUpdateTimeout.TotalMinutes:N0} minutes. Update cancelled.");
                TryKillProcessTree(process);
                return;
            }

            if (process.ExitCode != 0)
            {
                Console.WriteLine("Press key to continue...");
                Console.ReadKey();
            }
            
            Process.Start(packageName);
        }, packageNameOption, versionOption, processIdOption);
        return updateDotnetToolCommand;
    }

    private static async Task<bool> WaitForProcessExitAsync(int processId, TimeSpan timeout)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return await WaitForProcessExitAsync(process, timeout);
        }
        catch (ArgumentException)
        {
            // The application exited before the installer obtained its process handle.
            return true;
        }
    }

    private static async Task<bool> WaitForProcessExitAsync(Process process, TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
            return true;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return false;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (process.HasExited == false)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between checking HasExited and killing it.
        }
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
