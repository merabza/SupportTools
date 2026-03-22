using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.CreateDevDatabaseByMigration;

// ReSharper disable once UnusedType.Global
public class DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger<DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy(
        ILogger<DatabaseMigrationCreatorMigrationToolCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.CreateDevDatabaseByMigration);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var dmpCreator = DatabaseMigrationParameters.Create(_logger, _httpClientFactory, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (dmpCreator is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new DatabaseMigrationCreatorMigrationToolCommand(_logger, dmpCreator,
                    parametersManager)); //მიგრაციის საშუალებით ცარელა დეველოპერ ბაზის შექმნა
        }

        StShared.WriteErrorLine("dmpCreator is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
