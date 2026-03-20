using System.Net.Http;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.DropDevDatabase;

// ReSharper disable once UnusedType.Global
public class DatabaseDropperMigrationToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatabaseDropperMigrationToolCommandFactoryStrategy> _logger;

    public DatabaseDropperMigrationToolCommandFactoryStrategy(
        ILogger<DatabaseDropperMigrationToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.DropDevDatabase);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var dmpForDropper =
            DatabaseMigrationParameters.Create(_logger, _httpClientFactory, supportToolsParameters, projectName);
        if (dmpForDropper is not null)
        {
            return new DatabaseDropperMigrationToolCommand(_logger, dmpForDropper,
                parametersManager); //დეველოპერ ბაზის წაშლა
        }

        StShared.WriteErrorLine("dmpForDropper is null", true);
        return null;
    }
}
