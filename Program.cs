using System.CommandLine;
using mcli.commands;

var rootCommand = new RootCommand("mcli");

rootCommand.Subcommands.Add(VersionsCommand.Create());
rootCommand.Subcommands.Add(InstallCommand.Create());
rootCommand.Subcommands.Add(LaunchCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();