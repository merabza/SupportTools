using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class DeletePairedFieldCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeletePairedFieldCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base("Delete field pair", EMenuAction.LevelUp, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_menuParameters.ProjectName);
        if (project is null || string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName) ||
            string.IsNullOrWhiteSpace(_menuParameters.PairedTableKey) ||
            string.IsNullOrWhiteSpace(_menuParameters.PairedFieldKey))
        {
            StShared.WriteErrorLine("Project, pairs file, current table pair, or field pair not set", true);
            return false;
        }

        if (!Inputer.InputBool($"Delete field pair {_menuParameters.PairedFieldKey}?", false, false))
        {
            return false;
        }

        PairedDbObjectsModel result = PairedDbObjectsParametersManager.Load(project.PairedDbObjectsResultFileName, _logger);
        PairedTable? currentTable =
            result.PairedTables.FirstOrDefault(pt => PairedTableKeyBuilder.BuildKey(pt) == _menuParameters.PairedTableKey);
        if (currentTable is null)
        {
            StShared.WriteErrorLine($"Table pair {_menuParameters.PairedTableKey} not found", true);
            return false;
        }

        PairedField? toRemove =
            currentTable.PairedFields.FirstOrDefault(pf => PairedTableKeyBuilder.BuildFieldKey(pf) ==
                                                           _menuParameters.PairedFieldKey);
        if (toRemove is null)
        {
            StShared.WriteErrorLine($"Field pair {_menuParameters.PairedFieldKey} not found", true);
            return false;
        }

        currentTable.PairedFields.Remove(toRemove);

        var parMan = new PairedDbObjectsParametersManager(project.PairedDbObjectsResultFileName, result);
        bool saved = await parMan.Save(result, null, null, cancellationToken);
        if (!saved)
        {
            return false;
        }

        _menuParameters.PairedFieldKey = null;
        return true;
    }
}
