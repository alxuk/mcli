using System.CommandLine;
using System.Net;
using System.Text.Json;
using mcli.utils;
using System.IO.Compression;

namespace mcli.commands;

public static class InstallCommand
{
    public static Command Create()
    {
        var command = new Command("install")
        {
            Description = "Installs a new Minecraft instance."
        };

        command.SetAction(static async _ =>
        {
            Console.WriteLine("Choose your loader: \n[V]anilla");
            string choice = Console.ReadLine() ?? throw new Exception();

            switch (choice)
            {
                case "V" :
                    await VanillaInstall();
                    break;
                default:
                    Console.WriteLine("Invalid Option!");
                    throw new Exception();
            }
        });

        return command;
    }

    private static async Task VanillaInstall()
    {
        Console.Write("Instance Name: ");
        string instanceName = Console.ReadLine() ?? throw new Exception();
        Console.Write("Minecraft Version: ");
        string version = Console.ReadLine() ?? throw new Exception();

        JsonDocument manifest = await Versions.FetchVersions();

        bool exists = false;
        string url = "";
        foreach (var v in manifest.RootElement.GetProperty("versions").EnumerateArray())
        {
            string id = v.GetProperty("id").GetString()!;
            if (id == version)
            {
                exists = true;
                url = v.GetProperty("url").GetString()!;
                break;
            }
        }

        if (!exists)
        {
            Console.WriteLine("Invalid Version!");
            throw new Exception();
        }

        // Install client.jar
        Console.WriteLine("Installing client.jar...");
        string JarUrl = await Install.FetchJarUrl(url);
        string rootDir = Path.Combine(Utils.RootDir, instanceName);
        string outputPath = Path.Combine(rootDir, "client.jar");
        Directory.CreateDirectory(rootDir);

        using var http = new HttpClient();
        {
            await using var responseStream = await http.GetStreamAsync(JarUrl);
            await using var fileStream = File.Create(outputPath);
            await responseStream.CopyToAsync(fileStream);
        }

        // Install libraries
        Console.WriteLine("Installing libraries...");
        JsonElement libraries = await Install.FetchLibraries(url);

        foreach (JsonElement library in libraries.EnumerateArray())
        {
            string name = library.GetProperty("name").GetString() ?? "UNKNOWN";

            if (!library.TryGetProperty("downloads", out JsonElement downloads))
                continue;

            // Normal libraries
            if (downloads.TryGetProperty("artifact", out JsonElement artifact))
            {
                string relativePath = artifact.GetProperty("path").GetString() ?? throw new Exception();
                string libUrl = artifact.GetProperty("url").GetString() ?? throw new Exception();
                string path = Path.Combine(rootDir, "libraries", relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                await using var responseStream = await http.GetStreamAsync(libUrl);
                await using var fileStream = File.Create(path);
                await responseStream.CopyToAsync(fileStream);
            }

            // Cancerous natives
            if (downloads.TryGetProperty("classifiers", out JsonElement classifiers) && classifiers.TryGetProperty("natives-linux", out JsonElement native))
            {
                string relPath = native.GetProperty("path").GetString() ?? throw new Exception();
                string nativeUrl = native.GetProperty("url").GetString() ?? throw new Exception();
                string path = Path.Combine(rootDir, "libraries", relPath);

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                await using var responseStream = await http.GetStreamAsync(nativeUrl);
                await using var fileStream = File.Create(path);
                await responseStream.CopyToAsync(fileStream);

                string nativesDir = Path.Combine(rootDir, "natives");
                Directory.CreateDirectory(nativesDir);

                using var archive = ZipFile.OpenRead(path);

                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.StartsWith("META-INF/"))
                        continue;

                    string destination = Path.Combine(nativesDir, entry.FullName);

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    entry.ExtractToFile(destination, true);
                }
            }
        }

        // Install assets
        Console.WriteLine("Installing assets...");
        JsonDocument AssetJsonD = await Install.FetchAssets(url);

        string AssetIndexId = await Install.FetchAssetIndexId(url);
        string AssetIndexPath = Path.Combine(rootDir, "assets", "indexes");
        Directory.CreateDirectory(AssetIndexPath);

        string AssetIndexFile = Path.Combine(AssetIndexPath, $"{AssetIndexId}.json");
        await File.WriteAllTextAsync(AssetIndexFile, AssetJsonD.RootElement.GetRawText());

        JsonElement assets = AssetJsonD.RootElement.GetProperty("objects");

        using var semaphore = new SemaphoreSlim(8);
        var tasks = assets.EnumerateObject().Select(async asset =>
        {
            await semaphore.WaitAsync();
            try
            {
                string hash = asset.Value.GetProperty("hash").GetString()!;
                string prefix = hash[..2];

                string path = Path.Combine(rootDir, "assets", "objects", prefix, hash);

                if (File.Exists(path))
                    return;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                await using var response = await http.GetStreamAsync($"{MojangServer.Resources}/{prefix}/{hash}");
                await using var file = File.Create(path);
                await response.CopyToAsync(file);
            }
            finally { semaphore.Release(); }
        });
        await Task.WhenAll(tasks);

        Console.WriteLine("Finished installation!");
    }
}