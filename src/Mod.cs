using GMSL;
using UndertaleModLib;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Reflection;

namespace Shellworks_AutoUpdater;

public class Mod : IGMSLMod
{

    private static readonly HttpClient client = new();
    private JsonNode? json;
    private static readonly string shellworksDirectory = Path.Combine(Environment.CurrentDirectory, "Shellworks");

    public void Load(UndertaleData data)
    {
        if (CheckOutdated())
            UpdateShellworks();
        
        LoadShellworks(data);
    }

    private bool CheckOutdated()
    {
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        client.DefaultRequestHeaders.Add("User-Agent", "Shellworks updater");

        var task = Task.Run(() => client.GetAsync("https://api.github.com/repos/rgc-exists/Shellworks/releases/latest"));
        task.Wait();
        using HttpResponseMessage response = task.Result;
        response.EnsureSuccessStatusCode();
        var task2 = Task.Run(() => response.Content.ReadAsStringAsync());
        task2.Wait();
        json = JsonSerializer.Deserialize<JsonNode>(task2.Result);

        var versionFile = Path.Combine(Environment.CurrentDirectory, "version.txt");

        if (File.Exists(versionFile))
        {
            return File.ReadAllText(versionFile) != json["tag_name"].GetValue<string>();
        }
        else
            return true;
    }

    private void UpdateShellworks()
    {
        if (!Directory.Exists(shellworksDirectory))
            Directory.CreateDirectory(shellworksDirectory);
        
        Console.WriteLine($"Downloading new shellworks version {json["tag_name"].GetValue<string>()}");
        using var task = Task.Run(() => client.GetStreamAsync(json["assets"][0]["browser_download_url"].GetValue<string>()));
        task.Wait();

        var shellworksZip = Path.Combine(shellworksDirectory, "shellworks.zip");

        var stream = new FileStream(shellworksZip, FileMode.Create);
        task.Result.CopyTo(stream);
        stream.Dispose();

        Console.WriteLine("Unzipping new shellworks");
        var extracted = Path.Combine(shellworksDirectory, "Extracted");

        if (!Directory.Exists(extracted))
            Directory.CreateDirectory(extracted);
        else
            Directory.Delete(extracted, true);

        System.IO.Compression.ZipFile.ExtractToDirectory(shellworksZip, extracted);

        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "version.txt"), json["tag_name"].GetValue<string>());
    }

    private void LoadShellworks(UndertaleData data)
    {
        Console.WriteLine("Loading shellworks");
        var extracted = Path.Combine(shellworksDirectory, "Extracted");
        var assembly = Assembly.LoadFrom(Path.Combine(extracted, Directory.GetDirectories(extracted)[0], "gs2ml", "mods", "Shellworks", "Shellworks.dll"));

        var type = assembly.GetType("EditorTweaks.EditorTweaks");

        type.GetMethod("Load").Invoke(Activator.CreateInstance(type), new object[] { 0, data });
    }
}