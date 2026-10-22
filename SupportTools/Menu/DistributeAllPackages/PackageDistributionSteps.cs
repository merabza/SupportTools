using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportToolsData.Models;

namespace SupportTools.Menu.DistributeAllPackages;

//ნაბიჯები ნამდვილი ბრძანებებით: Sync ... Gits V3, UploadedPackageChecker, ProjectBuildChecker, PackageSolutionChecker,
//PackageBuilder და PackageDistributor, ისევე როგორც პროექტის მენიუს ცალკეული ბრძანებები
internal sealed class PackageDistributionSteps : IPackageDistributionSteps
{
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PackageDistributionSteps(ILogger logger, string appName, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, SupportToolsMenuParameters menuParameters)
    {
        _logger = logger;
        _appName = appName;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    //ერთი პროექტის V3 სინქრონიზაცია მის git-ებს ყოველთვის კლონავს, remote პროექტისასაც (როგორც V2)
    public async Task<bool> SyncProjectGits(string projectName, string commitMessage,
        CancellationToken cancellationToken)
    {
        var syncProjectGitsToolAction = SyncMultipleProjectsGitsToolActionV3.Create(_logger, _httpClientFactory,
            _parametersManager, null, projectName, true, commitMessage);
        return await syncProjectGitsToolAction.Run(cancellationToken);
    }

    public Task<string?> GetUnchangedUploadedPackageVersion(string projectName, CancellationToken cancellationToken)
    {
        var uploadedPackageChecker = new UploadedPackageChecker(_logger, _httpClientFactory, _parametersManager);
        return uploadedPackageChecker.GetUnchangedUploadedVersion(projectName, cancellationToken);
    }

    //შედეგი ჩაიწერება მენიუს პარამეტრებში, ისევე როგორც Check ... build-ის ბრძანებებში. restore NuGet-ის HTTP
    //ქეშის გარეშე ეშვება: დამოკიდებულების პაკეტი ამავე გაშვებაში აიტვირთა და ქეშში ჩაწერილ ვერსიების სიაში ჯერ არ არის
    public ProjectBuildCheckResult? CheckBuild(string projectName, ProjectModel project, bool noIncremental,
        CancellationToken cancellationToken)
    {
        ProjectBuildChecker.CheckProjects(_appName, [new KeyValuePair<string, ProjectModel>(projectName, project)],
            _menuParameters, _logger, noIncremental, true, cancellationToken);
        return _menuParameters.ProjectBuildCheckResults.GetValueOrDefault(projectName);
    }

    public bool CheckPackageSolution(string projectName)
    {
        var packageSolutionChecker = new PackageSolutionChecker(_logger, _parametersManager, projectName);
        return packageSolutionChecker.CheckPackageSolution();
    }

    public bool BuildAndUploadPackage(string projectName)
    {
        var packageBuilder = new PackageBuilder(_logger, _parametersManager, projectName);
        return packageBuilder.BuildAndUploadPackage();
    }

    public Task<bool> DistributePackagesToProject(string projectName, IReadOnlyList<string> packageProjectNames,
        CancellationToken cancellationToken)
    {
        var packageDistributor = new PackageDistributor(_logger, _httpClientFactory, _parametersManager);
        return packageDistributor.DistributePackagesToProject(projectName, packageProjectNames, cancellationToken);
    }
}
