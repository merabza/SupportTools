using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.ToolCommands.CorrectNewDatabase;
using LibDatabaseWork.ToolCommands.CreateDevDatabaseByMigration;
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
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatabaseReCreatorMigrationToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseReCreatorMigrationToolCommandFactoryStrategy(string appName,
        ILogger<DatabaseReCreatorMigrationToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _appName = appName;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.RecreateDevDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var dmpForReCreator = DatabaseMigrationParameters.Create(_appName, _logger, _httpClientFactory,
            supportToolsParameters, projectName);
        var correctNewDbParametersForRecreate =
            CorrectNewDbParameters.Create(_logger, supportToolsParameters, projectName);
        if (dmpForReCreator is null)
        {
            StShared.WriteErrorLine("dmpForReCreator is null", true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        if (project.DevDatabaseParameters == null)
        {
            StShared.WriteErrorLine($"DevDatabaseParameters is not specified for Project with name {projectName}",
                true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);
        var apiClients = new ApiClients(supportToolsParameters.ApiClients);

        if (correctNewDbParametersForRecreate is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(new DatabaseReCreatorMigrationToolCommand(_appName, _logger,
                dmpForReCreator, project.DevDatabaseParameters, correctNewDbParametersForRecreate,
                databaseServerConnections, apiClients, _httpClientFactory,
                parametersManager)); //დეველოპერ ბაზის წაშლა და თავიდან შექმნა
        }

        StShared.WriteErrorLine("correctNewDbParametersForRecreate is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
