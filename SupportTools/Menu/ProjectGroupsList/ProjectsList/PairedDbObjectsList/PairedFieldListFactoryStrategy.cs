using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class PairedFieldListFactoryStrategy(
    IServiceProvider serviceProvider,
    ILogger<PairedFieldListFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandListFactoryStrategy
{
    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(menuParameters.ProjectName);

        if (project is null || string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName) ||
            string.IsNullOrWhiteSpace(menuParameters.PairedTableKey))
        {
            return [];
        }

        PairedDbObjectsModel result = PairedDbObjectsParametersManager.Load(project.PairedDbObjectsResultFileName, logger);
        PairedTable? currentTable =
            result.PairedTables.FirstOrDefault(pt => PairedTableKeyBuilder.BuildKey(pt) == menuParameters.PairedTableKey);
        if (currentTable is null)
        {
            return [];
        }

        return currentTable.PairedFields.Select(CliMenuCommand (pf) =>
        {
            string key = PairedTableKeyBuilder.BuildFieldKey(pf);
            string displayName = $"{pf.ProdCopyFieldName} <-> {pf.DevFieldName}";
            return new PairedFieldSubMenuCliMenuCommand(serviceProvider, key, displayName,
                menuParameters);
        }).ToList();
    }
}
