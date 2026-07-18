using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.DistributeAllPackages;

public sealed class DistributeAllPackagesCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Distribute all packages";

    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DistributeAllPackagesCliMenuCommand(ILogger logger, string appName, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, SupportToolsMenuParameters menuParameters) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _appName = appName;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            "This process will sync gits, check build, check solution, build and distribute packages for all package type projects, ordered by package dependencies");
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //პაკეტის ტიპის ყველა პროექტის შეგროვება
        Dictionary<string, ProjectModel> packageProjects = parameters.Projects
            .Where(x => x.Value.ProjectType == EProjectType.IsPackage)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

        if (packageProjects.Count == 0)
        {
            StShared.WriteWarningLine("No package type projects found", true, _logger);
            return true;
        }

        //ჯერ მუშავდება ის პაკეტები, რომლებიც სხვა პაკეტებზე არ არიან დამოკიდებული,
        //შემდეგ კი ისინი, რომელთა ყველა დამოკიდებულებაც უკვე დამუშავებულია
        List<string> orderedProjectNames = OrderPackageProjectsByDependencies(packageProjects);
        if (orderedProjectNames.Count != packageProjects.Count)
        {
            List<string> cycleProjectNames = packageProjects.Keys.Except(orderedProjectNames, StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal).ToList();
            StShared.WriteErrorLine(
                $"Cannot order package projects by dependencies, cycle detected. Projects in cycle: {string.Join(", ", cycleProjectNames)}",
                true, _logger);
            return false;
        }

        Console.WriteLine($"Package projects processing order: {string.Join(", ", orderedProjectNames)}");

        int processedCount = 0;
        foreach (string projectName in orderedProjectNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Processing package project {projectName} ({processedCount + 1}/{orderedProjectNames.Count})");

            //ნებისმიერი ნაბიჯის ჩავარდნა აჩერებს პროექტების დამუშავების მთლიან ციკლს
            if (!await ProcessPackageProject(projectName, packageProjects[projectName], cancellationToken))
            {
                StShared.WriteErrorLine(
                    $"Distribute all packages stopped on project {projectName}. Successfully processed {processedCount} of {orderedProjectNames.Count} package projects",
                    true, _logger);
                return false;
            }

            processedCount++;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Distribute all packages finished. Package projects processed: {ProcessedCount}",
                processedCount);
        }

        Console.WriteLine($"Distribute all packages finished. Package projects processed: {processedCount}");
        return true;
    }

    //ერთი პაკეტის პროექტის სრული დამუშავება: სინქრონიზაცია, build-ის შემოწმება, სოლუშენის შემოწმება,
    //პაკეტის დაბილდვა, გავრცელება და ხელახალი სინქრონიზაცია
    private async Task<bool> ProcessPackageProject(string projectName, ProjectModel project,
        CancellationToken cancellationToken)
    {
        //გიტების სინქრონიზაცია
        if (!await SyncProjectGits(projectName, cancellationToken))
        {
            StShared.WriteErrorLine($"Sync gits failed for project {projectName}", true, _logger);
            return false;
        }

        //პროექტის build-ის შემოწმება
        ProjectBuildChecker.CheckProjects(_appName, [new KeyValuePair<string, ProjectModel>(projectName, project)],
            _menuParameters, _logger, cancellationToken);
        if (!_menuParameters.ProjectBuildCheckStatuses.TryGetValue(projectName, out EProjectBuildCheckStatus status) ||
            status != EProjectBuildCheckStatus.Success)
        {
            StShared.WriteErrorLine($"Build check failed for project {projectName} with status {status}", true,
                _logger);
            return false;
        }

        //პაკეტის სოლუშენის შემოწმება
        var packageSolutionChecker = new PackageSolutionChecker(_logger, _parametersManager, projectName);
        if (!packageSolutionChecker.CheckPackageSolution())
        {
            StShared.WriteErrorLine($"Package solution check failed for project {projectName}", true, _logger);
            return false;
        }

        //პაკეტის დაბილდვა და ატვირთვა package manager-ზე
        var packageBuilder = new PackageBuilder(_logger, _parametersManager, projectName);
        if (!packageBuilder.BuildAndUploadPackage())
        {
            StShared.WriteErrorLine($"Build package failed for project {projectName}", true, _logger);
            return false;
        }

        //პაკეტის გავრცელება ყველა მომხმარებელ პროექტში
        var packageDistributor = new PackageDistributor(_logger, _httpClientFactory, _parametersManager, projectName);
        if (!await packageDistributor.DistributePackage(cancellationToken))
        {
            StShared.WriteErrorLine($"Package distribution failed for project {projectName}", true, _logger);
            return false;
        }

        //გიტების ხელახალი სინქრონიზაცია
        if (!await SyncProjectGits(projectName, cancellationToken))
        {
            StShared.WriteErrorLine($"Final sync gits failed for project {projectName}", true, _logger);
            return false;
        }

        return true;
    }

    private async Task<bool> SyncProjectGits(string projectName, CancellationToken cancellationToken)
    {
        var syncProjectGitsToolAction =
            SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, null, projectName, true);
        return await syncProjectGitsToolAction.Run(cancellationToken);
    }

    //Kahn-ის ალგორითმი: პაკეტი დამოკიდებულია იმ პაკეტებზე, რომელთა რეპოზიტორიებიც მის გიტების სიაშია
    //(პაკეტის რეპოზიტორიის სახელი ემთხვევა პროექტის სახელს, არაპაკეტური რეპოზიტორიები დამოკიდებულებად არ ითვლება).
    //თუ წრიული დამოკიდებულებაა, დაბრუნებული სია არასრულია
    private static List<string> OrderPackageProjectsByDependencies(Dictionary<string, ProjectModel> packageProjects)
    {
        var dependents = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var remainingDependencyCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (string projectName in packageProjects.Keys)
        {
            dependents[projectName] = new HashSet<string>(StringComparer.Ordinal);
            remainingDependencyCounts[projectName] = 0;
        }

        foreach ((string projectName, ProjectModel project) in packageProjects)
        {
            foreach (string gitProjectName in project.GitProjectNames)
            {
                if (gitProjectName == projectName || !packageProjects.ContainsKey(gitProjectName))
                {
                    continue;
                }

                //დუბლირებული ჩანაწერი დამოკიდებულებას მეორედ არ ითვლის
                if (dependents[gitProjectName].Add(projectName))
                {
                    remainingDependencyCounts[projectName]++;
                }
            }
        }

        //SortedSet უზრუნველყოფს, რომ ერთდროულად მზა პროექტები ალფაბეტური თანმიმდევრობით დამუშავდეს
        var readyProjectNames = new SortedSet<string>(
            remainingDependencyCounts.Where(x => x.Value == 0).Select(x => x.Key), StringComparer.Ordinal);

        var orderedProjectNames = new List<string>(packageProjects.Count);
        while (readyProjectNames.Count > 0)
        {
            string projectName = readyProjectNames.First();
            readyProjectNames.Remove(projectName);
            orderedProjectNames.Add(projectName);

            foreach (string dependentProjectName in dependents[projectName])
            {
                remainingDependencyCounts[dependentProjectName]--;
                if (remainingDependencyCounts[dependentProjectName] == 0)
                {
                    readyProjectNames.Add(dependentProjectName);
                }
            }
        }

        return orderedProjectNames;
    }
}
