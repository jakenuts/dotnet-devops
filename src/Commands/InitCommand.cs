using System.Diagnostics.CodeAnalysis;
using devops.Internal;
using Spectre.Console;
using Spectre.Console.Cli;

namespace devops.Commands;

public class InitCommand(
    IAnsiConsole console,
    DevOpsConfigurationStore configStore,
    DevOpsConfigurationAccessor configurationAccessor)
    : Command<InitCommand.Settings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        console.WriteLine(
            "To connect to AzureDevops we'll need the url to your project collection and a PAT authorized to view it");
        console.WriteLine("Examples:");
        console.WriteLine("Url - https://mycompany.visualstudio.com/defaultcollection");
        console.WriteLine("PAT - apatfromazuredevopswhichisalongstringofcharacters");
        console.WriteLine("");

        var uri = console.Ask<string>("Enter DevOps [green]Url[/] :");

        var pat = console.Ask<string>("Enter DevOps [green]PAT[/] :");

        var update = new DevOpsConfiguration
        {
            CollectionUri = uri,
            CollectionPAT = pat
        };

        // Save it to disk
        configStore.SaveConfiguration(update, Constants.SettingsPath);

        // Update the loaded settings
        configurationAccessor.UpdateSettings(update);

        console.WriteLine($"Updated encrypted devops settings at '${Constants.SettingsPath}'.");
        console.WriteLine("");

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
    }
}