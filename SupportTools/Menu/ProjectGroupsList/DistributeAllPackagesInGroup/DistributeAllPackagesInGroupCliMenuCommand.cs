using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.DistributeAllPackages;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.DistributeAllPackagesInGroup;

//Distribute all packages მხოლოდ იმ პაკეტებზე, რომლებსაც ამ ჯგუფის პროექტები იყენებენ, და მხოლოდ ამ ჯგუფის პროექტებში
public sealed class DistributeAllPackagesInGroupCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Distribute all packages in group projects";

    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;
    private readonly IPackageDistributionSteps _steps;

    public DistributeAllPackagesInGroupCliMenuCommand(ILogger logger, string appName,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters, string projectGroupName) : this(logger, parametersManager,
        projectGroupName, RemoteOnlyProjects.InputBoolFromConsole,
        new PackageDistributionSteps(logger, appName, httpClientFactory, parametersManager, menuParameters))
    {
    }

    //კონსოლიდან შეყვანა და გარე ნაბიჯები (git, dotnet, პაკეტების სერვერი) პარამეტრებადაა გამოტანილი,
    //რომ ტესტებმა ისინი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal DistributeAllPackagesInGroupCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectGroupName, Func<string, bool, bool> inputBool, IPackageDistributionSteps steps) : base(
        MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
        _inputBool = inputBool;
        _steps = steps;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            $"This process will sync gits, check build, check solution, build and distribute packages for the package type projects whose repositories are in the gits lists of the projects of group {_projectGroupName} (in any group), ordered by package dependencies, and distribute them only to the projects of this group. Packages that have not changed since their last upload are not built and uploaded again");
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მხოლოდ ამ ჯგუფის პროექტები: მათი გიტების სიები ირჩევს პაკეტებს, რომელ ჯგუფშიც არ უნდა იყვნენ ისინი,
        //ბოლოს კი ამ პაკეტებს მხოლოდ ამ ჯგუფის არაპაკეტური პროექტები იღებენ
        var distributor = new AllPackagesDistributor(_logger, _inputBool, _steps);
        return await distributor.Run(parameters,
            parameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName),
            cancellationToken);
    }
}
