using System.Text.Json.Nodes;
using System.Text.Json;
using System.Reflection;

public class Updater {
    private static readonly HttpClient client = new();
    private static JsonNode? json;
    private static string shellworksDirectory = "";
    private static string shellworks_modName = "Shellworks";
    private static string wysPath = "";
    static void Main(string[] args)
    {
        wysPath = args[0];
        Console.WriteLine("Updating shellworks...");

        shellworksDirectory = Path.Combine(wysPath, "gmsl", "mods", shellworks_modName);
        
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

        if(Directory.Exists(Path.Combine(shellworksDirectory, "code"))){
            Directory.Delete(Path.Combine(shellworksDirectory, "code"), true);
        }
        if(File.Exists(Path.Combine(shellworksDirectory, "needsToBeUpdated.txt"))){
            File.Delete(Path.Combine(shellworksDirectory, "needsToBeUpdated.txt"));
        }

        System.IO.Compression.ZipFile.ExtractToDirectory(shellworksZip, extracted, true);

        File.WriteAllText(Path.Combine(wysPath, "Shellworks_AutoUpdater", "shellworks_version.txt"), json["tag_name"].GetValue<string>());

        LaunchGame();
    }

    static void LaunchGame(){
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = Path.Combine(wysPath, "Will You Snail.exe");
        process.StartInfo.Arguments = "-gmsl_console";
        process.Start();
        Environment.Exit(0);
    }
}