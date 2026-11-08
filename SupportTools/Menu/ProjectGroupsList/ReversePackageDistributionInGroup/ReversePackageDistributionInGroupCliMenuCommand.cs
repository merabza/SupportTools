using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ReversePackageDistribution;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ReversePackageDistributionInGroup;

//Reverse Package Distribution მხოლოდ ამ ჯგუფის პროექტებზე
public sealed class ReversePackageDistributionInGroupCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Reverse Package Distribution In Group Projects";

    private readonly Func<IProjectReferencesEditor> _createReferencesEditor;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;

    public ReversePackageDistributionInGroupCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectGroupName) : this(logger, parametersManager, projectGroupName,
        () => new DotnetProjectReferencesEditor(new DotnetProcessor(logger, true)))
    {
    }

    //csproj-ის ცვლილებები პარამეტრადაა გამოტანილი, რომ ტესტებმა dotnet არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal ReversePackageDistributionInGroupCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectGroupName, Func<IProjectReferencesEditor> createReferencesEditor) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
        _createReferencesEditor = createReferencesEditor;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            $"This process will replace PackageReferences of own packages with ProjectReferences in main repositories of the projects of group {_projectGroupName}");
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მხოლოდ ამ ჯგუფის პროექტები მუშავდება. remote პროექტების მთავარი რეპოზიტორია ამ კომპიუტერზე არ არის (D3)
        List<KeyValuePair<string, ProjectModel>> groupProjects = RemoteOnlyProjects.Skip(
            parameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName), _logger);

        var distributor = new ReversePackageDistributor(_logger, _createReferencesEditor());
        return ValueTask.FromResult(distributor.Run(parameters, groupProjects, cancellationToken));
    }
}
