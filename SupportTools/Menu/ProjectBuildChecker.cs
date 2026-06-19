using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Menu;

//"Check ... build" მენიუს ბრძანებების საერთო ლოგიკა - პროექტების დაბილდვა და სტატუსების მეხსიერებაში ჩაწერა
public static class ProjectBuildChecker
{
    public static void CheckProjects(IEnumerable<KeyValuePair<string, ProjectModel>> projects,
        SupportToolsMenuParameters menuParameters, ILogger logger, CancellationToken cancellationToken)
    {
        var dotnetProcessor = new DotnetProcessor(logger, true);

        foreach ((string projectName, ProjectModel project) in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EProjectBuildCheckStatus status = CheckProjectBuild(project, dotnetProcessor);
            menuParameters.ProjectBuildCheckStatuses[projectName] = status;
            Console.WriteLine($"{projectName}: {status}");
        }
    }

    private static EProjectBuildCheckStatus CheckProjectBuild(ProjectModel project, DotnetProcessor dotnetProcessor)
    {
        if (string.IsNullOrWhiteSpace(project.SolutionFileName))
        {
            return EProjectBuildCheckStatus.SolutionFileNameIsEmpty;
        }

        if (!File.Exists(project.SolutionFileName))
        {
            return EProjectBuildCheckStatus.SolutionFiledoesNotExists;
        }

        if (!IsSolutionFile(project.SolutionFileName))
        {
            return EProjectBuildCheckStatus.InvalidSolutionFile;
        }

        var buildResult = dotnetProcessor.Build(project.SolutionFileName);
        return buildResult.IsNone ? EProjectBuildCheckStatus.Success : EProjectBuildCheckStatus.BuildFailed;
    }

    private static bool IsSolutionFile(string solutionFileName)
    {
        string extension = Path.GetExtension(solutionFileName);
        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }
}
