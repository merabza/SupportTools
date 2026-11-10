using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.DistributeAllPackages;

//„Distribute all packages“-ის საერთო ლოგიკა. მთავარი მენიუს, ჯგუფისა და პროექტის ბრძანებები მას თავიანთ პროექტებს
//აწვდიან: ყველა პროექტს, ჯგუფის პროექტებს ან ერთ პროექტს
internal sealed class AllPackagesDistributor
{
    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IPackageDistributionSteps _steps;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AllPackagesDistributor(ILogger logger, Func<string, bool, bool> inputBool, IPackageDistributionSteps steps)
    {
        _logger = logger;
        _inputBool = inputBool;
        _steps = steps;
    }

    //ბრძანების პროექტებიდან ითვლება მხოლოდ UseLocalPackages-ჩართულები. მუშავდება პაკეტის ტიპის ის პროექტები,
    //რომლებიც მათ შორისაა ან რომელთა რეპოზიტორიებსაც მათი გიტების სიები ასახელებს, რომელ ჯგუფშიც არ უნდა იყვნენ.
    //ბოლოს ამ გაშვების პაკეტებს იღებენ ბრძანების არაპაკეტური პროექტები. მთავარი მენიუს ბრძანება ყველა პროექტს აწვდის,
    //ამიტომ მასში ყველა პაკეტი მუშავდება და ყველა არაპაკეტური პროექტი იღებს მათ
    public async Task<bool> Run(SupportToolsParameters parameters,
        IEnumerable<KeyValuePair<string, ProjectModel>> projects, CancellationToken cancellationToken)
    {
        List<KeyValuePair<string, ProjectModel>> scopeProjects = [.. projects.Where(x => x.Value.UseLocalPackages)];
        var scopeGitProjectNames = new HashSet<string>(
            scopeProjects.SelectMany(x => x.Value.GitProjectNames.Prepend(x.Key)), StringComparer.Ordinal);

        //პაკეტის ტიპის პროექტების შეგროვება. remote პაკეტები მხოლოდ ცხადი თანხმობით მუშავდება, რადგან
        //სინქრონიზაცია მათ ამ კომპიუტერზე დაკლონავს (D3). UseLocalPackages-გამორთული პროექტები არ მუშავდება
        List<KeyValuePair<string, ProjectModel>> allPackageProjects =
        [
            .. parameters.Projects.Where(x =>
                x.Value.ProjectType == EProjectType.IsPackage && x.Value.UseLocalPackages &&
                scopeGitProjectNames.Contains(x.Key))
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
        List<string> unchangedProjectNames = [];
        List<string> failedProjectNames = [];
        List<string> skippedProjectNames = [];
        for (int i = 0; i < orderedProjectNames.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string projectName = orderedProjectNames[i];
            ProjectModel project = packageProjects[projectName];

            //ჩავარდნილ ან გამოტოვებულ პაკეტზე დამოკიდებული პაკეტი გამოიტოვება: ის მისი ძველი ვერსიით არ უნდა აიგოს
            List<string> notDistributedDependencies =
            [
                .. failedProjectNames.Concat(skippedProjectNames).Where(x => project.GitProjectNames.Contains(x))
            ];
            if (notDistributedDependencies.Count > 0)
            {
                StShared.WriteWarningLine(
                    $"Package project {projectName} is skipped, because it uses package projects that were not distributed: {string.Join(", ", notDistributedDependencies)}",
                    true, _logger);
                skippedProjectNames.Add(projectName);
                continue;
            }

            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Processing package project {projectName} ({i + 1}/{orderedProjectNames.Count})");

            //ნაბიჯის ჩავარდნისას ეს პაკეტი გამოიტოვება და დამუშავება დანარჩენი პაკეტებით გრძელდება
            if (!await ProcessPackageProject(projectName, project, distributedProjectNames, unchangedProjectNames,
                    cancellationToken))
            {
                StShared.WriteWarningLine(
                    $"Package project {projectName} failed and is skipped, distribution goes on with the other package projects",
                    true, _logger);
                failedProjectNames.Add(projectName);
                continue;
            }

            processedCount++;
            distributedProjectNames.Add(projectName);
        }

        bool distributedToOtherProjects =
            await DistributePackagesToOtherProjects(scopeProjects, distributedProjectNames, cancellationToken);

        if (failedProjectNames.Count > 0)
        {
            string skippedText = skippedProjectNames.Count == 0
                ? string.Empty
                : $". Skipped because they use failed packages: {string.Join(", ", skippedProjectNames)}";
            StShared.WriteErrorLine(
                $"Distribute all packages finished with errors. Successfully processed {processedCount} of {orderedProjectNames.Count} package projects. Failed: {string.Join(", ", failedProjectNames)}{skippedText}",
                true, _logger);
            return false;
        }

        if (!distributedToOtherProjects)
        {
            return false;
        }

        int unchangedCount = unchangedProjectNames.Count;
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Distribute all packages finished. Package projects processed: {ProcessedCount}, built and uploaded: {UploadedCount}, unchanged since the last upload: {UnchangedCount}",
                processedCount, processedCount - unchangedCount, unchangedCount);
        }

        Console.WriteLine(
            $"Distribute all packages finished. Package projects processed: {processedCount}, built and uploaded: {processedCount - unchangedCount}, unchanged since the last upload: {unchangedCount}");
        return true;
    }

    //ერთი პაკეტის პროექტის სრული დამუშავება: ამ გაშვების პაკეტების მიღება, სინქრონიზაცია, build-ის შემოწმება,
    //სოლუშენის შემოწმება, პაკეტის დაბილდვა და ატვირთვა, ხელახალი სინქრონიზაცია. ბოლო ატვირთვის მერე შეუცვლელი პროექტი
    //სინქრონიზაციის შემდეგ მხოლოდ სოლუშენის შემოწმებას გადის და unchangedProjectNames-ში ემატება
    private async Task<bool> ProcessPackageProject(string projectName, ProjectModel project,
        IReadOnlyCollection<string> distributedProjectNames, List<string> unchangedProjectNames,
        CancellationToken cancellationToken)
    {
        //ამ გაშვების პაკეტები, რომლებსაც პროექტი იყენებს, დამოკიდებულებების რიგით მის წინ დამუშავდა და უკვე დაფუშულია.
        //ამიტომ მათი კლონები პროექტის გვერდით საბოლოო მდგომარეობამდე ახლდება და restore ძველ და ახალ კლონებს
        //ერთად ვერ ნახავს
        List<string> usedPackageNames = GetUsedPackageNames(project, distributedProjectNames);
        if (usedPackageNames.Count > 0 &&
            !await _steps.DistributePackagesToProject(projectName, usedPackageNames, cancellationToken))
        {
            StShared.WriteErrorLine($"Package distribution failed for project {projectName}", true, _logger);
            return false;
        }

        //გიტების სინქრონიზაცია
        if (!await _steps.SyncProjectGits(projectName, CreateSyncCommitMessage(projectName, usedPackageNames),
                cancellationToken))
        {
            StShared.WriteErrorLine($"Sync gits failed for project {projectName}", true, _logger);
            return false;
        }

        //ბოლო ატვირთვის მერე შეუცვლელი პროექტის build აღარ მოწმდება, მისი პაკეტები აღარ იბილდება და აღარ იტვირთება:
        //სერვერზე მათი უახლესი ვერსია რჩება, რომელსაც მომხმარებელი პროექტები იღებენ. სოლუშენის შემოწმება მაინც
        //სრულდება, რადგან ის ამ კომპიუტერის NuGet-ის კონფიგურაციაში პაკეტების წყაროსაც ამოწმებს
        string? unchangedVersion = await _steps.GetUnchangedUploadedPackageVersion(projectName, cancellationToken);
        if (unchangedVersion is not null)
        {
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Project {projectName} has not changed since its packages {unchangedVersion} were uploaded, build check, build and upload are skipped");
            if (!CheckPackageSolution(projectName))
            {
                return false;
            }

            unchangedProjectNames.Add(projectName);
            return true;
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
        if (!CheckPackageSolution(projectName))
        {
            return false;
        }

        //პაკეტის დაბილდვა და ატვირთვა package manager-ზე
        if (!_steps.BuildAndUploadPackage(projectName))
        {
            StShared.WriteErrorLine($"Build package failed for project {projectName}", true, _logger);
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

    private bool CheckPackageSolution(string projectName)
    {
        if (_steps.CheckPackageSolution(projectName))
        {
            return true;
        }

        StShared.WriteErrorLine($"Package solution check failed for project {projectName}", true, _logger);
        return false;
    }

    //ბრძანების არაპაკეტური პროექტები ამ გაშვების პაკეტებს ბოლოს იღებენ, როცა ყველა პაკეტი უკვე დაფუშულია. მათი
    //ცვლილებები დაუკომიტებელი რჩება. ერთი პროექტის ჩავარდნა დანარჩენებს არ აჩერებს
    private async Task<bool> DistributePackagesToOtherProjects(
        IEnumerable<KeyValuePair<string, ProjectModel>> scopeProjects,
        IReadOnlyCollection<string> distributedProjectNames, CancellationToken cancellationToken)
    {
        List<(string ProjectName, List<string> UsedPackageNames)> projects =
        [
            .. scopeProjects.Where(x => x.Value.ProjectType != EProjectType.IsPackage)
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => (ProjectName: x.Key,
                    UsedPackageNames: GetUsedPackageNames(x.Value, distributedProjectNames)))
                .Where(x => x.UsedPackageNames.Count > 0)
        ];

        List<string> failedProjectNames = [];
        for (int i = 0; i < projects.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string projectName, List<string> usedPackageNames) = projects[i];
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Distributing packages to project {projectName} ({i + 1}/{projects.Count})");

            if (!await _steps.DistributePackagesToProject(projectName, usedPackageNames, cancellationToken))
            {
                StShared.WriteErrorLine($"Package distribution failed for project {projectName}", true, _logger);
                failedProjectNames.Add(projectName);
            }
        }

        if (failedProjectNames.Count == 0)
        {
            return true;
        }

        StShared.WriteErrorLine(
            $"Distribute all packages finished with errors. Packages were not distributed to projects: {string.Join(", ", failedProjectNames)}",
            true, _logger);
        return false;
    }

    //ამ გაშვების ის პაკეტები, რომელთა რეპოზიტორიებიც პროექტის გიტების სიაშია, დამუშავების რიგით
    private static List<string> GetUsedPackageNames(ProjectModel project, IEnumerable<string> distributedProjectNames)
    {
        return [.. distributedProjectNames.Where(x => project.GitProjectNames.Contains(x))];
    }

    //პირველი სინქრონიზაციის commit-ის მესიჯი. პროექტის git-ებში ცვლილებებს ძირითადად სინქრონიზაციის წინ მიღებული
    //ამ გაშვების პაკეტები ტოვებს, ამიტომ მესიჯი მათ ასახელებს
    internal static string CreateSyncCommitMessage(string projectName, IReadOnlyCollection<string> usedPackageNames)
    {
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
