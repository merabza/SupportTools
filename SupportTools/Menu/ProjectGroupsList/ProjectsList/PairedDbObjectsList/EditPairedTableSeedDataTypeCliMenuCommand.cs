using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class EditPairedTableSeedDataTypeCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditPairedTableSeedDataTypeCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base("Edit seed data type", EMenuAction.Reload, EMenuAction.Reload,
        null, false, EStatusView.Table)
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

        PairedDbObjectsResult result = PairedDbObjectsFileLoader.Load(project.PairedDbObjectsResultFileName, _logger);
        PairedTable? current =
            result.PairedTables.FirstOrDefault(pt =>
                PairedTableKeyBuilder.BuildKey(pt) == _menuParameters.PairedTableKey);
        if (current is null)
        {
            StShared.WriteErrorLine($"Table pair {_menuParameters.PairedTableKey} not found in file", true);
            return ValueTask.FromResult(false);
        }

        ESeedDataType newValue = MenuInputer.InputFromEnumList("Seed Data Type", current.SeedDataType);
        current.SeedDataType = newValue;

        bool saved = PairedDbObjectsFileLoader.Save(project.PairedDbObjectsResultFileName, result, _logger);
        return ValueTask.FromResult(saved);
    }

    protected override string? GetStatus()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_menuParameters.ProjectName);
        if (project is null || string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName) ||
            string.IsNullOrWhiteSpace(_menuParameters.PairedTableKey))
        {
            return null;
        }

        PairedDbObjectsResult result = PairedDbObjectsFileLoader.Load(project.PairedDbObjectsResultFileName, _logger);
        PairedTable? current =
            result.PairedTables.FirstOrDefault(pt =>
                PairedTableKeyBuilder.BuildKey(pt) == _menuParameters.PairedTableKey);

        return current?.SeedDataType.ToString();
    }
}
