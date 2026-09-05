using System.CommandLine;
using System.Text.Json;
using mcli.utils;
using System.Diagnostics;
using System.IO.Compression;

namespace mcli.commands;

public static class LaunchCommand
{
    public static Command Create()
    {
        var command = new Command("launch")
        {
            Description = "Launches a Minecraft instance."
        };

        command.SetAction(async _ =>
        {
            await LaunchGame();
        });

        return command;
    }

    private static async Task LaunchGame()
    {   
        Console.Write("Instance to launch: ");
        string instance = Console.ReadLine() ?? throw new Exception();
        
        string rootDir = Utils.RootDir;

        if (!Directory.Exists(Path.Combine(rootDir, instance)))
        {
            throw new Exception("Invalid Instance!");
        }

        Console.Write("Username: ");
        string username = Console.ReadLine() ?? throw new Exception();

        string gameDir = Path.Combine(rootDir, instance);
        string indexesDir = Path.Combine(gameDir, "assets", "indexes");
        string assetIndex = Path.GetFileNameWithoutExtension(Directory.GetFiles(indexesDir, "*.json").Single());

        var jars = Directory.GetFiles(
            Path.Combine(gameDir, "libraries"),
            "*.jar",
            SearchOption.AllDirectories
        );
        string clientJar = Path.Combine(gameDir, "client.jar");
        string cp = string.Join(
            Path.PathSeparator,
            new[] { clientJar }.Concat(jars)
        );

        using var archive = ZipFile.OpenRead(clientJar);
        var versionEntry = archive.GetEntry("version.json") ?? throw new Exception();
        using var stream = versionEntry.Open();
        using var json = JsonDocument.Parse(stream);
        string version = json.RootElement.GetProperty("id").GetString() ?? throw new Exception();

        var psi = new ProcessStartInfo
        {
            FileName = "java",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("-cp");
        psi.ArgumentList.Add(cp);

        psi.ArgumentList.Add("net.minecraft.client.main.Main");

        psi.ArgumentList.Add("--username");
        psi.ArgumentList.Add(username);

        psi.ArgumentList.Add("--gameDir");
        psi.ArgumentList.Add(gameDir);

        psi.ArgumentList.Add("--assetsDir");
        psi.ArgumentList.Add(Path.Combine(gameDir, "assets"));

        psi.ArgumentList.Add("--assetIndex");
        psi.ArgumentList.Add(assetIndex);

        psi.ArgumentList.Add("--accessToken");
        psi.ArgumentList.Add("0"); // TODO: add online support

        psi.ArgumentList.Add("--version");
        psi.ArgumentList.Add(version);

        using var process = Process.Start(psi)!;

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        Console.WriteLine(output);
        Console.Error.WriteLine(error);

        Console.WriteLine($"Exit code: {process.ExitCode}");
    }
}