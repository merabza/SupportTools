using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class PairedTableListFactoryStrategy(
    IServiceProvider serviceProvider,
    ILogger<PairedTableListFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandListFactoryStrategy
{
    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(menuParameters.ProjectName);

        if (project is null || string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName))
        {
            return [];
        }

        PairedDbObjectsResult result = PairedDbObjectsFileLoader.Load(project.PairedDbObjectsResultFileName, logger);

        return result.PairedTables.Select(pt =>
        {
            string key = PairedTableKeyBuilder.BuildKey(pt);
            string displayName =
                $"{pt.ProdCopySchemaName}.{pt.ProdCopyTableName} <-> {pt.DevSchemaName}.{pt.DevTableName} ({pt.PairedFields.Count} fields) [SeedDataType: {pt.SeedDataType}]";
            return (CliMenuCommand)new PairedTableSubMenuCliMenuCommand(serviceProvider, key, displayName,
                menuParameters);
        }).ToList();
    }
}
