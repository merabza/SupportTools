using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Menu;

//"Check ... build" მენიუს ბრძანებების საერთო ლოგიკა - პროექტების დაბილდვა და შედეგების მეხსიერებაში ჩაწერა
public static class ProjectBuildChecker
{
    public static void CheckProjects(string appName, IEnumerable<KeyValuePair<string, ProjectModel>> projects,
        SupportToolsMenuParameters menuParameters, ILogger logger, CancellationToken cancellationToken)
    {
        var dotnetProcessor = new DotnetProcessor(logger, true);

        foreach ((string projectName, ProjectModel project) in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectBuildCheckResult result = CheckProjectBuild(projectName, appName, project, dotnetProcessor);
            menuParameters.ProjectBuildCheckResults[projectName] = result;
            ProjectBuildCheckStatusView.WriteResultLine(projectName, result);
        }
    }

    private static ProjectBuildCheckResult CheckProjectBuild(string projectName, string appName,
        ProjectModel project, DotnetProcessor dotnetProcessor)
    {
        if (string.IsNullOrWhiteSpace(project.SolutionFileName))
        {
            return new ProjectBuildCheckResult(EProjectBuildCheckStatus.SolutionFileNameIsEmpty);
        }

        if (!File.Exists(project.SolutionFileName))
        {
            return new ProjectBuildCheckResult(EProjectBuildCheckStatus.SolutionFileDoesNotExists);
        }

        if (!IsSolutionFile(project.SolutionFileName))
        {
            return new ProjectBuildCheckResult(EProjectBuildCheckStatus.InvalidSolutionFile);
        }

        if (projectName == appName)
        {
            return new ProjectBuildCheckResult(EProjectBuildCheckStatus.CannotBuildSelf);
        }

        DotnetBuildResult buildResult = dotnetProcessor.Build(project.SolutionFileName);
        return new ProjectBuildCheckResult(GetBuildStatus(buildResult), buildResult.ErrorCount,
            buildResult.WarningCount);
    }

    //ჩავარდნილი build - BuildFailed, წარმატებული გაფრთხილებებით - SuccessWithWarnings, სხვაგვარად - Success
    private static EProjectBuildCheckStatus GetBuildStatus(DotnetBuildResult buildResult)
    {
        if (!buildResult.Succeeded)
        {
            return EProjectBuildCheckStatus.BuildFailed;
        }

        return buildResult.WarningCount > 0
            ? EProjectBuildCheckStatus.SuccessWithWarnings
            : EProjectBuildCheckStatus.Success;
    }

    private static bool IsSolutionFile(string solutionFileName)
    {
        string extension = Path.GetExtension(solutionFileName);
        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }
}
