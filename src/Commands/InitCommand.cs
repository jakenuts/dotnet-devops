using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using devops.Internal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common.CommandLine;
using Spectre.Console;
using Spectre.Console.Cli;

namespace devops.Commands;

public class InitCommand : Command<InitCommand.Settings>
{
    private readonly IAnsiConsole _console;

    private readonly DevOpsConfigurationProtector _protector;

    private readonly DevOpsConfigurationAccessor _configurationAccessor;

    public sealed class Settings : CommandSettings
    {
    }

    public InitCommand(IAnsiConsole console, DevOpsConfigurationProtector protector, DevOpsConfigurationAccessor configurationAccessor)
    {
        _console = console;
        _protector = protector;
        _configurationAccessor = configurationAccessor;
    }

    private void SaveConfiguration(DevOpsConfiguration config)
    {
        var saveConfig = new DevOpsConfiguration()
        {
            CollectionPAT = config.CollectionPAT,
            CollectionUri = config.CollectionUri
        };

        _protector.Encrypt(saveConfig);

        var configAsJson = JsonSerializer.Serialize(config);
        var configFile = "{\"DevOps\":" + configAsJson + "\n}";

        if (!Directory.Exists(Constants.SettingsDirectory))
        {
            Directory.CreateDirectory(Constants.SettingsDirectory);
        }

        var path = Constants.SettingsPath;
        File.WriteAllText(path, configFile);

        _console.WriteLine($"Updated encrypted devops settings at '${path}'.");
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        

        _console.WriteLine("To connect to AzureDevops we'll need the url to your project collection and a PAT authorized to view it");
        _console.WriteLine("Examples:");
        _console.WriteLine("Url - https://mycompany.visualstudio.com/defaultcollection");
        _console.WriteLine("PAT - apatfromazuredevopswhichisalongstringofcharacters");
        _console.WriteLine("");

        var uri = _console.Ask<string>("Enter DevOps [green]Url[/] :");

        var pat = _console.Ask<string>("Enter DevOps [green]PAT[/] :");

        var update = new DevOpsConfiguration
        {
            CollectionUri = uri,
            CollectionPAT = pat
        };

        _configurationAccessor.OverrideSettings(update);

       
        SaveConfiguration(update);

        _console.WriteLine("");

        return 0;
    }

}