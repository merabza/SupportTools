﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.BaseCopier.ServerBaseToServerCopier;

//ამ სერვერის მიმდინარე ბაზის დაკოპირება იმავე პროექტის არჩეული სერვერის ახალ ბაზაში, შუალედური (ProdCopy) ბაზის
//გარეშე. მიზნის სერვერი კონსოლიდან აირჩევა
// ReSharper disable once UnusedType.Global
public sealed class ServerBaseToServerCopierFactoryStrategy : BaseCopierFactoryStrategy, IToolCommandFactoryStrategy
{
    private readonly Func<string, List<string>, string?> _selectDestinationServer;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerBaseToServerCopierFactoryStrategy(IApplication app,
        ILogger<ServerBaseToServerCopierFactoryStrategy> logger, IHttpClientFactory httpClientFactory) : this(app,
        logger, httpClientFactory, SelectDestinationServer)
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    internal ServerBaseToServerCopierFactoryStrategy(IApplication app, ILogger logger,
        IHttpClientFactory httpClientFactory, Func<string, List<string>, string?> selectDestinationServer) : base(app,
        logger, httpClientFactory)
    {
        _selectDestinationServer = selectDestinationServer;
    }

    public string ToolCommandName => nameof(EProjectServerTools.ServerBaseToServerCopier);

    public async ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

        if (serverInfo.CurrentDatabaseParameters == null)
        {
            StShared.WriteErrorLine($"CurrentDatabaseParameters is not specified {serverInfo.ServerName}", true);
            return null;
        }

        DatabaseParameters? destinationDatabaseParameters = SelectDestinationDatabaseParameters(project, serverInfo);
        if (destinationDatabaseParameters is null)
        {
            return null;
        }

        return await CreateToolCommand(parametersManager, serverInfo.CurrentDatabaseParameters,
            destinationDatabaseParameters, cancellationToken);
    }

    //მიზნად გამოდგება პროექტის ყველა სერვერი (ესეც), რომელსაც ახალი ბაზის პარამეტრები აქვს
    internal DatabaseParameters? SelectDestinationDatabaseParameters(ProjectModel project, ServerInfoModel serverInfo)
    {
        string sourceKey = serverInfo.GetItemKey();
        string? sourceDatabaseName = serverInfo.CurrentDatabaseParameters?.DatabaseName;

        Dictionary<string, DatabaseParameters> destinations = project.ServerInfos.Values
            .Where(x => x.NewDatabaseParameters is not null)
            .GroupBy(x => x.GetItemKey(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().NewDatabaseParameters!, StringComparer.OrdinalIgnoreCase);

        if (destinations.Count == 0)
        {
            StShared.WriteErrorLine(
                $"Project has no server with NewDatabaseParameters to copy database {sourceDatabaseName} of {sourceKey} to",
                true);
            return null;
        }

        string? destinationKey;
        try
        {
            destinationKey = _selectDestinationServer(
                $"Copy database {sourceDatabaseName} from server {sourceKey} to which server?",
                [.. destinations.Keys.Order(StringComparer.OrdinalIgnoreCase)]);
        }
        catch (DataInputException)
        {
            destinationKey = null;
        }

        if (destinationKey is null ||
            !destinations.TryGetValue(destinationKey, out DatabaseParameters? destinationDatabaseParameters))
        {
            StShared.WriteErrorLine("Destination server is not selected", true);
            return null;
        }

        Console.WriteLine(
            $"Database {sourceDatabaseName} of server {sourceKey} will be copied to database {destinationDatabaseParameters.DatabaseName} of server {destinationKey}");
        return destinationDatabaseParameters;
    }

    private static string? SelectDestinationServer(string fieldName, List<string> serverKeys)
    {
        var serversMenuSet = new CliMenuSet();
        foreach (string serverKey in serverKeys)
        {
            serversMenuSet.AddMenuItem(new CliMenuCommand(serverKey));
        }

        return MenuInputer.InputFromMenuList(fieldName, serversMenuSet);
    }
}
