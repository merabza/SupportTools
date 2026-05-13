using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class DeletePairedTableCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeletePairedTableCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base("Delete table pair", EMenuAction.LevelUp, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_menuParameters.ProjectName);
        if (project is null || string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName) ||
            string.IsNullOrWhiteSpace(_menuParameters.PairedTableKey))
        {
            StShared.WriteErrorLine("Project, pairs file, or current table pair not set", true);
            return ValueTask.FromResult(false);
        }

        if (!Inputer.InputBool($"Delete table pair {_menuParameters.PairedTableKey}?", false, false))
        {
            return ValueTask.FromResult(false);
        }

        PairedDbObjectsResult result = PairedDbObjectsFileLoader.Load(project.PairedDbObjectsResultFileName, _logger);
        PairedTable? toRemove =
            result.PairedTables.FirstOrDefault(pt => PairedTableKeyBuilder.BuildKey(pt) == _menuParameters.PairedTableKey);
        if (toRemove is null)
        {
            StShared.WriteErrorLine($"Table pair {_menuParameters.PairedTableKey} not found", true);
            return ValueTask.FromResult(false);
        }

        result.PairedTables.Remove(toRemove);

        if (!PairedDbObjectsFileLoader.Save(project.PairedDbObjectsResultFileName, result, _logger))
        {
            return ValueTask.FromResult(false);
        }

        _menuParameters.PairedTableKey = null;
        return ValueTask.FromResult(true);
    }
}
