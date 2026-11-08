using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ReversePackageDistribution;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ReversePackageDistributionInProject;

//Reverse Package Distribution მხოლოდ ამ ერთ პროექტზე
public sealed class ReversePackageDistributionInProjectCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Reverse Package Distribution In Project";

    private readonly Func<IProjectReferencesEditor> _createReferencesEditor;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    public ReversePackageDistributionInProjectCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName) : this(logger, parametersManager, projectName,
        () => new DotnetProjectReferencesEditor(new DotnetProcessor(logger, true)))
    {
    }

    //csproj-ის ცვლილებები პარამეტრადაა გამოტანილი, რომ ტესტებმა dotnet არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal ReversePackageDistributionInProjectCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName, Func<IProjectReferencesEditor> createReferencesEditor) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload, projectName, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _createReferencesEditor = createReferencesEditor;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            $"This process will replace PackageReferences of own packages with ProjectReferences in the main repository of project {_projectName}");
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true, _logger);
            return ValueTask.FromResult(false);
        }

        //მხოლოდ ეს ერთი პროექტი მუშავდება
        var distributor = new ReversePackageDistributor(_logger, _createReferencesEditor());
        return ValueTask.FromResult(distributor.Run(parameters,
            [new KeyValuePair<string, ProjectModel>(_projectName, project)], cancellationToken));
    }
}
