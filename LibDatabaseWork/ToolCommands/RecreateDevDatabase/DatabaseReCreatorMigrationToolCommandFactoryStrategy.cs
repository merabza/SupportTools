using System.Net.Http;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.RecreateDevDatabase;

// ReSharper disable once UnusedType.Global
public class DatabaseReCreatorMigrationToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatabaseReCreatorMigrationToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseReCreatorMigrationToolCommandFactoryStrategy(
        ILogger<DatabaseReCreatorMigrationToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.RecreateDevDatabase);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var dmpForReCreator =
            DatabaseMigrationParameters.Create(_logger, _httpClientFactory, supportToolsParameters, projectName);
        var correctNewDbParametersForRecreate =
            CorrectNewDbParameters.Create(_logger, supportToolsParameters, projectName);
        if (dmpForReCreator is null)
        {
            StShared.WriteErrorLine("dmpForReCreator is null", true);
            return null;
        }

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return null;
        }

        if (project.DevDatabaseParameters == null)
        {
            StShared.WriteErrorLine($"DevDatabaseParameters is not specified for Project with name {projectName}",
                true);
            return null;
        }

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);
        var apiClients = new ApiClients(supportToolsParameters.ApiClients);

        if (correctNewDbParametersForRecreate is not null)
        {
            return new DatabaseReCreatorMigrationToolCommand(_logger, dmpForReCreator, project.DevDatabaseParameters,
                correctNewDbParametersForRecreate, databaseServerConnections, apiClients, _httpClientFactory,
                parametersManager); //დეველოპერ ბაზის წაშლა და თავიდან შექმნა
        }

        StShared.WriteErrorLine("correctNewDbParametersForRecreate is null", true);
        return null;
    }
}
