using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.DistributeAllPackages;

public sealed class DistributeAllPackagesCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Distribute all packages";

    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly IPackageDistributionSteps _steps;

    public DistributeAllPackagesCliMenuCommand(ILogger logger, string appName, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, SupportToolsMenuParameters menuParameters) : this(logger,
        parametersManager, RemoteOnlyProjects.InputBoolFromConsole,
        new PackageDistributionSteps(logger, appName, httpClientFactory, parametersManager, menuParameters))
    {
    }

    //კონსოლიდან შეყვანა და გარე ნაბიჯები (git, dotnet, პაკეტების სერვერი) პარამეტრებადაა გამოტანილი,
    //რომ ტესტებმა ისინი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal DistributeAllPackagesCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        Func<string, bool, bool> inputBool, IPackageDistributionSteps steps) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _inputBool = inputBool;
        _steps = steps;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            "This process will sync gits, check build, check solution, build and distribute packages for all package type projects, ordered by package dependencies");
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //პაკეტის ტიპის ყველა პროექტის შეგროვება. remote პაკეტები მხოლოდ ცხადი თანხმობით მუშავდება, რადგან
        //სინქრონიზაცია მათ ამ კომპიუტერზე დაკლონავს (D3). UseLocalPackages-გამორთული პროექტები არ მუშავდება
        List<KeyValuePair<string, ProjectModel>> allPackageProjects =
        [
            .. parameters.Projects.Where(x =>
                x.Value.ProjectType == EProjectType.IsPackage && x.Value.UseLocalPackages)
        ];
        bool includeRemoteProjects = RemoteOnlyProjects.AskInclude(allPackageProjects, _inputBool);
        Dictionary<string, ProjectModel> packageProjects = ProjectPresence
            .Filter(allPackageProjects, includeRemoteProjects)
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
            List<string> cycleProjectNames =
            [
                .. packageProjects.Keys.Except(orderedProjectNames, StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
            ];
            StShared.WriteErrorLine(
                $"Cannot order package projects by dependencies, cycle detected. Projects in cycle: {string.Join(", ", cycleProjectNames)}",
                true, _logger);
            return false;
        }

        Console.WriteLine($"Package projects processing order: {string.Join(", ", orderedProjectNames)}");

        int processedCount = 0;
        List<string> distributedProjectNames = [];
        foreach (string projectName in orderedProjectNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Processing package project {projectName} ({processedCount + 1}/{orderedProjectNames.Count})");

            //ნებისმიერი ნაბიჯის ჩავარდნა აჩერებს პროექტების დამუშავების მთლიან ციკლს
            if (!await ProcessPackageProject(projectName, packageProjects[projectName], distributedProjectNames,
                    cancellationToken))
            {
                StShared.WriteErrorLine(
                    $"Distribute all packages stopped on project {projectName}. Successfully processed {processedCount} of {orderedProjectNames.Count} package projects",
                    true, _logger);
                return false;
            }

            processedCount++;
            distributedProjectNames.Add(projectName);
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
        IReadOnlyCollection<string> distributedProjectNames, CancellationToken cancellationToken)
    {
        //გიტების სინქრონიზაცია
        if (!await _steps.SyncProjectGits(projectName,
                CreateSyncCommitMessage(projectName, project, distributedProjectNames), cancellationToken))
        {
            StShared.WriteErrorLine($"Sync gits failed for project {projectName}", true, _logger);
            return false;
        }

        //პროექტის build-ის შემოწმება (ინკრემენტული, კითხვის გარეშე). გაფრთხილებებით აგებული პროექტის გავრცელება არ ჩერდება
        ProjectBuildCheckResult? buildCheckResult = _steps.CheckBuild(projectName, project, false, cancellationToken);
        if (buildCheckResult is null || !buildCheckResult.IsBuildSucceeded)
        {
            StShared.WriteErrorLine(
                $"Build check failed for project {projectName} with status {ProjectBuildCheckStatusView.GetText(buildCheckResult)}",
                true, _logger);
            return false;
        }

        //პაკეტის სოლუშენის შემოწმება
        if (!_steps.CheckPackageSolution(projectName))
        {
            StShared.WriteErrorLine($"Package solution check failed for project {projectName}", true, _logger);
            return false;
        }

        //პაკეტის დაბილდვა და ატვირთვა package manager-ზე
        if (!_steps.BuildAndUploadPackage(projectName))
        {
            StShared.WriteErrorLine($"Build package failed for project {projectName}", true, _logger);
            return false;
        }

        //პაკეტის გავრცელება ყველა მომხმარებელ პროექტში
        if (!await _steps.DistributePackage(projectName, cancellationToken))
        {
            StShared.WriteErrorLine($"Package distribution failed for project {projectName}", true, _logger);
            return false;
        }

        //გიტების ხელახალი სინქრონიზაცია
        if (!await _steps.SyncProjectGits(projectName, $"Distribute all packages: after {projectName} package",
                cancellationToken))
        {
            StShared.WriteErrorLine($"Final sync gits failed for project {projectName}", true, _logger);
            return false;
        }

        return true;
    }

    //პირველი სინქრონიზაციის commit-ის მესიჯი. პროექტის git-ებში ცვლილებებს ძირითადად ამავე გაშვებაში უკვე
    //გავრცელებული პაკეტები ტოვებს, რომლებსაც პროექტი იყენებს, ამიტომ მესიჯი მათ ასახელებს
    internal static string CreateSyncCommitMessage(string projectName, ProjectModel project,
        IEnumerable<string> distributedProjectNames)
    {
        List<string> usedPackageNames =
            [.. distributedProjectNames.Where(x => project.GitProjectNames.Contains(x))];
        return usedPackageNames.Count == 0
            ? $"Distribute all packages: before {projectName} package"
            : $"Distribute all packages: use new {string.Join(", ", usedPackageNames)} packages in {projectName}";
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
