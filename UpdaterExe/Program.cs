using System.Text.Json.Nodes;
using System.Text.Json;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class Updater
{
    private static readonly HttpClient client = new();
    private static JsonNode? json;
    private static string shellworksDirectory = "";
    private static string shellworks_modName = "Shellworks";
    private static string wysPath = "";
    private static string appDataDirectory = "";

    public static bool requiresNet8 = false;
    static void Main(string[] args)
    {
        wysPath = args[0];
        Console.WriteLine("Updating shellworks...");

        shellworksDirectory = Path.Combine(wysPath, "gmsl", "mods", shellworks_modName);
        appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Will_You_Snail");

        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        client.DefaultRequestHeaders.Add("User-Agent", "Shellworks updater");
        var task = Task.Run(() => client.GetAsync("https://api.github.com/repos/rgc-exists/Shellworks/releases/latest"));
        task.Wait();
        using HttpResponseMessage response = task.Result;
        response.EnsureSuccessStatusCode();
        var task2 = Task.Run(() => response.Content.ReadAsStringAsync());
        task2.Wait();
        json = JsonSerializer.Deserialize<JsonNode>(task2.Result);

        if (!Directory.Exists(shellworksDirectory))
            Directory.CreateDirectory(shellworksDirectory);

        Console.WriteLine($"Downloading new shellworks version {json["tag_name"].GetValue<string>()}");
        using var task3 = Task.Run(() => client.GetStreamAsync(json["assets"][0]["browser_download_url"].GetValue<string>()));
        task.Wait();

        var shellworksZip = Path.Combine(wysPath, "Shellworks_AutoUpdater", "shellworks_extracted_cache.zip");

        var stream = new FileStream(shellworksZip, FileMode.Create);
        task3.Result.CopyTo(stream);
        stream.Dispose();

        Console.WriteLine("Unzipping new shellworks");
        var extracted = Path.Combine(wysPath);

        if (Directory.Exists(Path.Combine(shellworksDirectory, "code")))
        {
            Directory.Delete(Path.Combine(shellworksDirectory, "code"), true);
        }
        if (File.Exists(Path.Combine(shellworksDirectory, "needsToBeUpdated.txt")))
        {
            File.Delete(Path.Combine(shellworksDirectory, "needsToBeUpdated.txt"));
        }
        if (File.Exists(Path.Combine(appDataDirectory, "Shellworks_Cache", "needsToBeUpdated.txt")))
        {
            File.Delete(Path.Combine(appDataDirectory, "Shellworks_Cache", "needsToBeUpdated.txt"));
        }

        System.IO.Compression.ZipFile.ExtractToDirectory(shellworksZip, extracted, true);

        File.WriteAllText(Path.Combine(wysPath, "Shellworks_AutoUpdater", "shellworks_version.txt"), json["tag_name"].GetValue<string>());

        while (requiresNet8 && !CheckDotnetVersion("8"))
        {
            Console.WriteLine(@"
██ ███    ███ ██████   ██████  ██████  ████████  █████  ███    ██ ████████ ██ 
██ ████  ████ ██   ██ ██    ██ ██   ██    ██    ██   ██ ████   ██    ██    ██ 
██ ██ ████ ██ ██████  ██    ██ ██████     ██    ███████ ██ ██  ██    ██    ██ 
██ ██  ██  ██ ██      ██    ██ ██   ██    ██    ██   ██ ██  ██ ██    ██       
██ ██      ██ ██       ██████  ██   ██    ██    ██   ██ ██   ████    ██    ██ 
                                                                              
Due to an update to UndertaleModLib, you MUST install .net 8 before continuing!
This is likely a one-time thing. It will not happen next update.
Apologies for the inconvenience.

Download link: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Would you like to open the link in your default browser? (type Y or N) > ");
            string answer = Console.ReadLine();
            if (answer.ToLower().StartsWith("y"))
            {
                string url = "https://dotnet.microsoft.com/en-us/download/dotnet/8.0";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }
        Console.WriteLine("Dotnet 8 is installed. Continuing...");

        LaunchGame();
    }


    static bool CheckDotnetVersion(string version)
    {
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "--list-runtimes",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processInfo))
        {
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (string.IsNullOrEmpty(output))
                {
                    Console.WriteLine(".NET Runtime is not installed.");
                }
                else
                {
                    string pattern = $@"Microsoft\.NETCore\.App {version}\.\d+";
                    return Regex.IsMatch(output, pattern);
                }
            }
            else
            {
                Console.WriteLine("Could not run the 'dotnet' command.");
            }
            return false;
        }
    }

    static void LaunchGame()
    {
        var process = new Process();
        process.StartInfo.FileName = Path.Combine(wysPath, "Will You Snail.exe");
        process.StartInfo.Arguments = "-gmsl_console";
        process.Start();
        Environment.Exit(0);
    }
}