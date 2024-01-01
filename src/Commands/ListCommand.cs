using System.Collections.Concurrent;
using System.ComponentModel;
using devops.Internal;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Spectre.Console;
using Spectre.Console.Cli;

namespace devops.Commands;

public class ListCommand(IAnsiConsole console, DevOpsConfigurationAccessor devoptions)
    : AuthorizedCommandBase<ListCommand.Settings>(console, devoptions)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var authResult = await base.ExecuteAsync(context, settings);

        if (authResult < 0 || VssConnection == null)
        {
            return authResult;
        }

        var projectClient = VssConnection.GetClient<ProjectHttpClient>();
        var buildClient = VssConnection.GetClient<BuildHttpClient>();

        var projects = await projectClient.GetProjects();

        var data = new ConcurrentDictionary<TeamProjectReference, List<string[]>>();

        await Console.Progress()
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task1 = ctx.AddTask("[green] Checking projects..[/]", true, projects.Count);

                await Parallel.ForEachAsync(projects, CancellationToken.None, async (project, token) =>
                {
                    task1.Increment(1);

                    var builds = await buildClient.GetBuildsAsync(project.Name,
                        queryOrder: BuildQueryOrder.QueueTimeDescending,
                        maxBuildsPerDefinition: 3,
                        cancellationToken: token);

                    if (!builds.Any())
                    {
                        return;
                    }

                    data[project] = GetBuildRows(settings, builds);
                });
            });

        // Create a table
        var table = new Table
        {
            Border = TableBorder.Rounded
        };

        // Add some columns
        //table.AddColumn("Project");
        table.AddColumn("Pipeline");
        table.AddColumn("Build #");
        table.AddColumn("Status");
        table.AddColumn("Finished");

        foreach (var d in data.OrderBy(x => x.Key.Name))
        {
            if (d.Value.Any(x => x.Length > 0))
            {
                table.AddRow(d.Key.Name);

                foreach (var row in d.Value)
                {
                    table.AddRow(row);
                }

                table.AddEmptyRow();
            }
        }

        // Render the table to the console
        Console.Write(table);

        return 0;
    }

    private static string GetBuildResultEmoji(BuildResult? result)
    {
        if (result == null)
        {
            return "🏃";
        }

        switch (result.Value)
        {
            case BuildResult.None:
                return "❔";
            case BuildResult.Succeeded:
                return "✅"; //"✔";
            case BuildResult.PartiallySucceeded:
                return "⚠";
            case BuildResult.Failed:
                return "⛔";
            case BuildResult.Canceled:
                return "🛑";
        }

        return "❔";
    }

    private static List<string[]> GetBuildRows(Settings settings, List<Build> builds)
    {
        var grouped = builds.GroupBy(b => new
        {
            b.Definition.Id,
            b.Definition.Name
        }).OrderBy(x => x.Key.Name).ToList();

        var rows = new List<string[]>();

        foreach (var g in grouped)
        {
            // Queued
            // Running
            // Finished

            var runs = g.OrderByDescending(x => x.QueueTime).Take(1);

            foreach (var b in runs)
            {
                if (settings.Failed == true && b.Result != BuildResult.Failed)
                {
                    continue;
                }

                if (settings.Recent == true && b.QueueTime.HasValue)
                {
                    var ago = DateTime.UtcNow - b.QueueTime.Value;

                    if (ago.TotalHours > 24.0)
                    {
                        continue;
                    }
                }

                rows.Add(new[]
                {
                    GetBuildResultEmoji(b.Result) + " " + g.Key.Name,
                    b.BuildNumber,
                    b.Status?.ToString() ?? " ",
                    b.FinishTime?.ToString() ?? ""
                });
            }
        }

        return rows;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-f|--failed")]
        [DefaultValue(false)]
        public bool? Failed { get; set; }

        [CommandOption("-r|--recent")]
        [DefaultValue(false)]
        public bool? Recent { get; set; }
    }
}