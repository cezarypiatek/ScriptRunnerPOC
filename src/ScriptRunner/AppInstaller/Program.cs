using System.Diagnostics;
using System.IO.Compression;
using System.Net;

internal class Program
{

    public static async Task Main(string[] args)
    {
        var startingProcess = args[0];
        var downloadPath = args[1];

        try
        {
            using var wc = new HttpClient();
            var stream = await wc.GetStreamAsync(downloadPath);
            var destination = Path.GetDirectoryName(startingProcess);
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
    }
}