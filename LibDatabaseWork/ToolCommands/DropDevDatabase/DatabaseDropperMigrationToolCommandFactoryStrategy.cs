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
public class DatabaseDropperMigrationToolCommandFactoryStrategy(
    IApplication app,
    ILogger<DatabaseDropperMigrationToolCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectTools.DropDevDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var dmpForDropper = DatabaseMigrationParameters.Create(app.AppName, logger, httpClientFactory,
            supportToolsParameters, projectToolsFactoryStrategyParameters.ProjectName);
        if (dmpForDropper is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(new DatabaseDropperMigrationToolCommand(logger, dmpForDropper,
                parametersManager)); //დეველოპერ ბაზის წაშლა
        }

        StShared.WriteErrorLine("dmpForDropper is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
