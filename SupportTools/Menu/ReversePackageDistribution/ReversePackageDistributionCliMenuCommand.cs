using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ReversePackageDistribution;

public sealed class ReversePackageDistributionCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Reverse Package Distribution";

    private readonly Func<IProjectReferencesEditor> _createReferencesEditor;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public ReversePackageDistributionCliMenuCommand(ILogger logger, IParametersManager parametersManager) : this(logger,
        parametersManager, () => new DotnetProjectReferencesEditor(new DotnetProcessor(logger, true)))
    {
    }

    //csproj-ის ცვლილებები პარამეტრადაა გამოტანილი, რომ ტესტებმა dotnet არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal ReversePackageDistributionCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        Func<IProjectReferencesEditor> createReferencesEditor) : base(MenuCommandName, EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _createReferencesEditor = createReferencesEditor;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            "This process will replace PackageReferences of own packages with ProjectReferences in main repositories of all projects");
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //remote პროექტების მთავარი რეპოზიტორია ამ კომპიუტერზე არ არის (D3)
        List<KeyValuePair<string, ProjectModel>> projects = RemoteOnlyProjects.Skip(parameters.Projects, _logger);

        var distributor = new ReversePackageDistributor(_logger, _createReferencesEditor());
        return ValueTask.FromResult(distributor.Run(parameters, projects, cancellationToken));
    }
}
