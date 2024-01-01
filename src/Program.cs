using System.Diagnostics;
using System.Text;
using Community.Extensions.Spectre.Cli.Hosting;
using devops.Commands;
using devops.Internal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;

Console.OutputEncoding = Encoding.Default;

var builder = Host.CreateApplicationBuilder(args);



#region ⚙️ Configuration

builder.Configuration.AddJsonFile(Constants.SettingsPath, true, true);

#endregion

#region 📰 Logging

builder.Logging.AddSimpleConsole(opts => { opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; });

builder.Logging.AddFilter((cat, level) =>
{
#if DEBUG
    return level > LogLevel.Trace;
#else
    if (cat?.StartsWith("Microsoft") == true)
    {
        return level > LogLevel.Information;
    }

    return level > LogLevel.Debug;
#endif
});

#endregion

#region 🎾 Services

builder.Services.AddDataProtection()
    .SetApplicationName(Constants.SettingsAppName)
    .DisableAutomaticKeyGeneration();

builder.Services.Configure<DevOpsConfiguration>(builder.Configuration.GetSection("DevOps"));
builder.Services.AddTransient<DevOpsConfigurationProtector>();
builder.Services.AddTransient<DevOpsConfigurationStore>();
builder.Services.ConfigureOptions<DevOpsConfigurationProtector>();
builder.Services.AddTransient<IValidateOptions<DevOpsConfiguration>, DevOpsConfigurationValidation>();
builder.Services.AddTransient<DevOpsConfigurationAccessor>();

#endregion

#region 🐶 Commands

builder.Services.AddCommand<InitCommand>("init");
builder.Services.AddCommand<ListCommand>("list");
builder.Services.AddCommand<WatchCommand>("watch");

builder.UseSpectreConsole(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.SetApplicationName(Constants.AppName);
    config.UseBasicExceptionHandler();
});

#endregion

#region Stopping on Ctrl-C
//builder.Services.AddSingleton<IHostLifetime, ConsoleLifetime>();
builder.Services.Configure<HostOptions>(opts =>
{
    opts.ShutdownTimeout = TimeSpan.FromSeconds(1);
});
#endregion

var app = builder.Build();

run:

await app.RunAsync();

if (Environment.ExitCode == Constants.UnauthorizedExitCode)
{
    var cmdApp = app.Services.GetRequiredService<ICommandApp>();
    Environment.ExitCode = await cmdApp.RunAsync(new[] { "init" });
    goto run;
}

#if DEBUG
if (Debugger.IsAttached)
{
    await app.StopAsync(TimeSpan.FromSeconds(10));
    AnsiConsole.Ask<string>("Hit <Enter> to exit");
}
#endif

return Environment.ExitCode;