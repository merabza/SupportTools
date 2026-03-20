using System.Net.Http;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.CreateDevDatabaseByMigration;

// ReSharper disable once UnusedType.Global
public class DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectTools.CreateDevDatabaseByMigration);

    private readonly ILogger<DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy(ILogger<DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var dmpCreator =
            DatabaseMigrationParameters.Create(_logger, _httpClientFactory, supportToolsParameters, projectName);
        if (dmpCreator is not null)
        {
            return new DatabaseMigrationCreatorMigrationToolCommand(_logger, dmpCreator,
                parametersManager); //მიგრაციის საშუალებით ცარელა დეველოპერ ბაზის შექმნა
        }

        StShared.WriteErrorLine("dmpCreator is null", true);
        return null;
    }
}
