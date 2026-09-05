using System.CommandLine;
using System.Text.Json;
using mcli.utils;

namespace mcli.commands;

public static class VersionsCommand
{
    public static Command Create()
    {
        var command = new Command("versions")
        {
            Description = "Lists all available Minecraft versions"
        };

        command.SetAction(async _ =>
        {
            JsonDocument manifest = await Versions.FetchVersions();

            Console.WriteLine("Every Minecraft Version:");

            foreach (var version in manifest.RootElement.GetProperty("versions").EnumerateArray())
            {
                string id = version.GetProperty("id").GetString()!;
                string url = version.GetProperty("url").GetString()!;

                Console.WriteLine(id);
            }
        });

        return command;
    }
}