using AppCliTools.CliMenu;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class PairedDbObjectsSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedDbObjectsSubMenuCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName) : base("Paired Db Objects", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    public override CliMenuSet? GetSubMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} not found", true, _logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName))
        {
            StShared.WriteErrorLine(
                $"Project {_projectName} does not contain PairedDbObjectsResultFileName. Please set it in project parameters.",
                true, _logger);
            return null;
        }

        PairedDbObjectsModel model =
            PairedDbObjectsParametersManager.Load(project.PairedDbObjectsResultFileName, _logger);
        var pairedParMan = new PairedDbObjectsParametersManager(project.PairedDbObjectsResultFileName, model);

        var resolver = PairedDbObjectsConnectionResolver.Create(parameters, project, _logger);
        if (resolver is null)
        {
            return null;
        }

        var cruder = PairedTableCruder.Create(pairedParMan, _logger, model.PairedTables,
            resolver.ProdCopyConnectionString, resolver.DevConnectionString);
        return cruder.GetListMenu();
    }
}
