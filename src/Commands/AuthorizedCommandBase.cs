using devops.Internal;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Spectre.Console;
using Spectre.Console.Cli;

namespace devops.Commands;

public abstract class AuthorizedCommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    protected readonly IAnsiConsole _console;

    private readonly DevOpsConfigurationAccessor _devoptionsAccessor;

    protected VssConnection? VssConnection;

    protected AuthorizedCommandBase(IAnsiConsole console, DevOpsConfigurationAccessor devoptions)
    {
        _console = console;
        _devoptionsAccessor = devoptions;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        try
        {
            var config = _devoptionsAccessor.GetSettings();

            var devopsUri = new Uri(config.CollectionUri);
            var deopsCreds = new VssBasicCredential(string.Empty, config.CollectionPAT);
            VssConnection = new VssConnection(devopsUri, deopsCreds);

            await VssConnection.ConnectAsync();
        }
        catch (OptionsValidationException ex)
        {
            _console.WriteLine("Could not connect to Azure Devops - " + ex.Message);
            return Constants.UnauthorizedExitCode;
        }

        return 0;
    }
}