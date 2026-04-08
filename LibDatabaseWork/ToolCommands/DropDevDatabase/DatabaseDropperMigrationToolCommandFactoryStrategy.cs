using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.ToolCommands.CreateDevDatabaseByMigration;
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
    private readonly string _appName;
    private readonly ILogger<DatabaseDropperMigrationToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseDropperMigrationToolCommandFactoryStrategy(string appName,
        ILogger<DatabaseDropperMigrationToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _appName = appName;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.DropDevDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var dmpForDropper = DatabaseMigrationParameters.Create(_appName, _logger, _httpClientFactory, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (dmpForDropper is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(new DatabaseDropperMigrationToolCommand(_logger, dmpForDropper,
                parametersManager)); //დეველოპერ ბაზის წაშლა
        }

        StShared.WriteErrorLine("dmpForDropper is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
