using System.Diagnostics;
using System.Reflection;
using System.Text;
using devops.Commands;
using devops.Internal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Cli.Extensions.DependencyInjection;
using Spectre.Console.Cli;

Console.OutputEncoding = Encoding.Default;

#region Configuration

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Constants.SettingsPath, true, true)
    .AddCommandLine(args)
    .AddEnvironmentVariables()
    .Build();


#endregion

#region Services

var services = new ServiceCollection();
services.AddDataProtection().SetApplicationName(Constants.SettingsAppName).DisableAutomaticKeyGeneration();
services.AddSingleton<IConfiguration>(configuration);
services.Configure<DevOpsConfiguration>(configuration.GetSection("DevOps"));
services.AddTransient<DevOpsConfigurationProtector>();
services.ConfigureOptions<DevOpsConfigurationProtector>();
services.AddTransient<IValidateOptions<DevOpsConfiguration>, DevOpsConfigurationValidation>();
services.AddSingleton<DevOpsConfigurationAccessor>();
#endregion

#region Logging

services.AddLogging(logging =>
{
    logging.AddFilter((cat, level) =>
    {
        if (cat.StartsWith("Microsoft"))
        {
            return level > LogLevel.Information;
        }

        return level > LogLevel.Trace;
    });

    logging.AddSimpleConsole(opts => { opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; });
});

#endregion

if (args.Length == 0)
{
    //args = new[] { "watch" };
}

var registrar = new DependencyInjectionRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.AddCommand<InitCommand>("init");
    config.AddCommand<ListCommand>("list");
    config.AddCommand<WatchCommand>("watch");
});

run:


var exitCode = await app.RunAsync(args);

if (exitCode == Constants.UnauthorizedExitCode)
{
    var initCode = await app.RunAsync(new[] { "init" });

    goto run;

    //Console.WriteLine("Please run 'devops' again to use the new DevOps settings.");
}

#if DEBUG
if (Debugger.IsAttached)
{
    Console.WriteLine("Hit any key to exit");
    Console.ReadKey();
}
#endif

return exitCode;