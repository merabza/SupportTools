using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.DistributeAllPackages;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.DistributeAllPackagesInProject;

//Distribute all packages მხოლოდ იმ პაკეტებზე, რომლებსაც ეს პროექტი იყენებს, და მხოლოდ ამ პროექტში
public sealed class DistributeAllPackagesInProjectCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Distribute all packages in project";

    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly IPackageDistributionSteps _steps;

    public DistributeAllPackagesInProjectCliMenuCommand(ILogger logger, string appName,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters, string projectName) : this(logger, parametersManager, projectName,
        RemoteOnlyProjects.InputBoolFromConsole,
        new PackageDistributionSteps(logger, appName, httpClientFactory, parametersManager, menuParameters))
    {
    }

    //კონსოლიდან შეყვანა და გარე ნაბიჯები (git, dotnet, პაკეტების სერვერი) პარამეტრებადაა გამოტანილი,
    //რომ ტესტებმა ისინი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal DistributeAllPackagesInProjectCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName, Func<string, bool, bool> inputBool, IPackageDistributionSteps steps) : base(
        MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, projectName, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _inputBool = inputBool;
        _steps = steps;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            $"This process will sync gits, check build, check solution, build and distribute packages for the package type projects whose repositories are in the gits list of project {_projectName} (in any group), ordered by package dependencies, and distribute them only to this project. Packages that have not changed since their last upload are not built and uploaded again");
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true, _logger);
            return false;
        }

        //მხოლოდ ეს ერთი პროექტი: მისი გიტების სია ირჩევს პაკეტებს (თვითონაც, თუ ის პაკეტია), ბოლოს კი ამ პაკეტებს
        //მხოლოდ ეს პროექტი იღებს, თუ ის პაკეტი არ არის
        var distributor = new AllPackagesDistributor(_logger, _inputBool, _steps);
        return await distributor.Run(parameters, [new KeyValuePair<string, ProjectModel>(_projectName, project)],
            cancellationToken);
    }
}
