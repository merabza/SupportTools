using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.SyncV3;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace LibGitWork.OutdatedPackages;

public sealed class OutdatedPackagesUpdateSteps : IOutdatedPackagesUpdateSteps
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;
    private readonly IProgressConsole? _progressConsole;

    //გადამისამართებულ გამოტანაში (ფაილი, სხვა პროგრამა) დაფა არ იხატება, მხოლოდ შედეგების სტრიქონები იწერება
    public OutdatedPackagesUpdateSteps(ILogger? logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        Console.IsOutputRedirected ? null : new SystemProgressConsole())
    {
    }

    //progressConsole=null: პარალელური განახლება დაფის გარეშე, მხოლოდ სტრიქონებით
    // ReSharper disable once ConvertToPrimaryConstructor
    internal OutdatedPackagesUpdateSteps(ILogger? logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, IProgressConsole? progressConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _progressConsole = progressConsole;
    }

    //V3: ცვლილებების გარეშე, remote-ის ცნობილ ვერსიაზე მდგარი ფოლდერი git სერვერს არ მიმართავს, ამიტომ განახლების წინა და
    //შემდეგი სინქრონიზაცია მხოლოდ იმ ფოლდერებზე მუშაობს, რომლებიც წინა git-ის push-მა მოაძველა ან განახლებამ შეცვალა
    public Task<bool> SyncGits(string? projectName, bool includeRemoteProjects, string commitMessage,
        CancellationToken cancellationToken)
    {
        var syncMultipleProjectsGitsToolAction = SyncMultipleProjectsGitsToolActionV3.Create(_logger,
            _httpClientFactory, _parametersManager, null, projectName, includeRemoteProjects, commitMessage);
        return syncMultipleProjectsGitsToolAction.Run(cancellationToken);
    }

    //git-ის პარამეტრები დაფამდე, თანმიმდევრობით იქმნება: მათი შეცდომები (git ან პროექტი არ არის) ჩვეულებრივ იბეჭდება
    public IReadOnlyList<EPackageUpdateResult> UpdatePackages(IReadOnlyList<OutdatedPackagesUpdateStep> steps,
        CancellationToken cancellationToken)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var results = new EPackageUpdateResult[steps.Count];
        List<int> jobIndexes = [];
        List<PackageUpdateJob> jobs = [];
        for (int i = 0; i < steps.Count; i++)
        {
            OutdatedPackagesUpdateStep step = steps[i];
            var packageUpdaterToolAction =
                PackageUpdaterToolAction.Create(_logger, _parametersManager, step.ProjectName, step.GitName, true);
            if (packageUpdaterToolAction is null)
            {
                results[i] = EPackageUpdateResult.OutdatedFailed;
                continue;
            }

            jobIndexes.Add(i);
            //Create მხოლოდ მაშინ იქმნება, როცა პროექტს ფოლდერი აქვს
            jobs.Add(new PackageUpdateJob(step.GitName, parameters.GetGitsFolder(step.ProjectName)!,
                packageUpdaterToolAction.RunPackageUpdate));
        }

        ILogger? loggerOrNull = parameters.LogGitWork ? _logger : null;
        EPackageUpdateResult[] jobResults =
            new OutdatedPackagesParallelUpdater(loggerOrNull, _progressConsole).Run(jobs, cancellationToken);
        for (int i = 0; i < jobs.Count; i++)
        {
            results[jobIndexes[i]] = jobResults[i];
        }

        return results;
    }
}
