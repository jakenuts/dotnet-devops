﻿using System.Collections.Concurrent;
using System.ComponentModel;
using devops.Internal;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Spectre.Console;
using Spectre.Console.Cli;

namespace devops.Commands;

public class WatchCommand(IAnsiConsole console, DevOpsConfigurationAccessor devoptions)
    : AuthorizedCommandBase<WatchCommand.Settings>(console, devoptions)
{
    private BuildHttpClient? _buildClient;

    private ProjectHttpClient? _projectClient;

    private ReleaseHttpClient2? _releaseClient;

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var authResult = await base.ExecuteAsync(context, settings);

        if (authResult < 0 || VssConnection == null)
        {
            return authResult;
        }

        _projectClient = VssConnection.GetClient<ProjectHttpClient>();
        _buildClient = VssConnection.GetClient<BuildHttpClient>();
        _releaseClient = VssConnection.GetClient<ReleaseHttpClient2>();

        var projects = await _projectClient.GetProjects();

        while (true)
        {
            var buildsByProject = new ConcurrentDictionary<TeamProjectReference, List<Build>>();
            var releasesByProject = new ConcurrentDictionary<TeamProjectReference, List<Release>>();

            await Console.Progress()
                .HideCompleted(false)
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task1 = ctx.AddTask("[green] Checking projects for active builds..[/]", true, projects.Count);

                    while (true)
                    {
                        await Parallel.ForEachAsync(projects, CancellationToken.None, async (project, token) =>
                        {
                            task1.Increment(1);

                            if (!string.IsNullOrEmpty(settings.Project))
                            {
                                if (!project.Name.Contains(settings.Project, StringComparison.OrdinalIgnoreCase))
                                {
                                    return;
                                }
                            }

                            var builds = await _buildClient.GetBuildsAsync(project.Name,

                                //statusFilter:BuildStatus.InProgress,
                                queryOrder: BuildQueryOrder.QueueTimeDescending,
                                maxBuildsPerDefinition: 1,
                                cancellationToken: token);

                            var activeBuilds = builds.Where(IsActiveOrPending).ToList();

                            if (activeBuilds.Any())
                            {
                                buildsByProject[project] = builds;
                            }

                            /*
                            var releases = await _releaseClient.GetReleasesAsync2(
                                project.Id,
                                statusFilter: ReleaseStatus.Active,
                                top: 1, cancellationToken: token);

                            if (releases.Any())
                            {
                                releasesByProject[project] = releases.ToList();
                            }*/
                        });

                        if (settings.Continuous == true && buildsByProject.IsEmpty && releasesByProject.IsEmpty)
                        {
                            task1.Value = 0;

                            await Task.Delay(5000);
                        }
                        else
                        {
                            break;
                        }
                    }
                });

            if (buildsByProject.IsEmpty && releasesByProject.IsEmpty)
            {
                Console.WriteLine("All builds & releases complete!");
                return 0;
            }

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
            table.AddColumn("Checked");

            var buildRows = new ConcurrentDictionary<int, Build>();

            // Build initial rows
            foreach (var d in buildsByProject.OrderBy(x => x.Key.Name))
            {
                if (d.Value.Any())
                {
                    table.AddRow(d.Key.Name);

                    foreach (var build in d.Value)
                    {
                        buildRows[table.Rows.Count] = build;
                        table.AddRow(GetBuildRow(settings, build).ToArray());
                    }

                    table.AddEmptyRow();
                }
            }

            await Console.Live(table)
                .StartAsync(async ctx =>
                {
                    while (true)
                    {
                        await UpdateBuildStatus(buildRows.Values);

                        foreach (var d in buildRows)
                        {
                            var newRowData = GetBuildRow(settings, d.Value);

                            table.UpdateCell(d.Key, 2, newRowData[2]);
                            table.UpdateCell(d.Key, 3, newRowData[3]);
                            table.UpdateCell(d.Key, 4, DateTime.UtcNow.ToString("hh:mm:ss"));
                        }

                        ctx.Refresh();

                        var finished = buildRows.Where(entry =>
                            entry.Value.Status != BuildStatus.InProgress
                            && entry.Value.Status != BuildStatus.NotStarted).ToArray();

                        /*
                        foreach (var f in finished)
                        {
                            buildRows.Remove(f.Key, out var _);
                            Console.Beep();
                        }*/

                        var activeBuilds = buildRows.Any(entry => IsActiveOrPending(entry.Value) ||
                                                                  entry.Value.FinishTime > DateTime.UtcNow.AddMinutes(-1));

                        if (activeBuilds == false)
                        {
                            return 0;
                        }

                        await Task.Delay(1000);
                    }
                });

            if (settings.Continuous == false)
            {
                break;
            }

            await Task.Delay(5000);
        }

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

    private static List<string> GetBuildRow(Settings settings, Build build)
    {
        var row = new List<string>();

        row.AddRange(new[]
        {
            GetBuildResultEmoji(build.Result) + " " + build.Definition.Name,
            build.BuildNumber,
            GetBuildStatusText(build.Status, build.Result),
            build.FinishTime?.ToString() ?? ""
        });

        return row;
    }

    private static string GetBuildStatusText(BuildStatus? status, BuildResult? result)
    {
        return result switch
        {
            BuildResult.Failed => "Failed",
            BuildResult.Canceled => "Cancelled",
            _ => status?.ToString() ?? ""
        };
    }

    protected bool IsActiveOrPending(Build build) => build.Status == BuildStatus.InProgress ||
                                                     build.Status == BuildStatus.NotStarted &&
                                                     build.QueueTime >= DateTime.UtcNow.AddMinutes(-10);

    private static void MakeSound(BuildResult? result)
    {
        if (!OperatingSystem.IsWindows())
        {
            System.Console.Beep();
            return;
        }

        switch (result)
        {
            case BuildResult.Succeeded:
                System.Console.Beep(800, 200);
                break;
            case BuildResult.PartiallySucceeded:
                System.Console.Beep();
                break;
            case BuildResult.Failed:
                System.Console.Beep(240, 200);
                break;
            case BuildResult.Canceled:
                System.Console.Beep(300, 200);
                break;
        }
    }

    private async Task UpdateBuildStatus(ICollection<Build> builds)
    {
        if (_buildClient == null)
        {
            return;
        }

        await Parallel.ForEachAsync(builds, CancellationToken.None, async (build, token) =>
        {
            var updatedBuild = await _buildClient.GetBuildAsync(
                build.Project.Id, build.Id,

                //propertyFilters:
                cancellationToken: token);

            // Status Changed
            if (build.Status == BuildStatus.InProgress && updatedBuild.Status != BuildStatus.InProgress)
            {
                MakeSound(updatedBuild.Result);
            }

            build.BuildNumber = updatedBuild.BuildNumber;
            build.Status = updatedBuild.Status;
            build.Result = updatedBuild.Result;
            build.FinishTime = updatedBuild.FinishTime;
        });
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--project")]
        public string? Project { get; set; }

        [CommandOption("-c|--continuous")]
        [DefaultValue(true)]
        public bool? Continuous { get; set; }
    }
}