using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

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
        Func<string, bool, bool> inputBool, IPackageDistributionSteps steps) : base(MenuCommandName, EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _inputBool = inputBool;
        _steps = steps;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            "This process will sync gits, check build, check solution, build and distribute packages for all package type projects, ordered by package dependencies. Packages that have not changed since their last upload are not built and uploaded again");
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ყველა პროექტი: მუშავდება პაკეტის ტიპის ყველა პროექტი, ბოლოს კი მათ ყველა არაპაკეტური პროექტი იღებს
        var distributor = new AllPackagesDistributor(_logger, _inputBool, _steps);
        return await distributor.Run(parameters, parameters.Projects, cancellationToken);
    }
}
