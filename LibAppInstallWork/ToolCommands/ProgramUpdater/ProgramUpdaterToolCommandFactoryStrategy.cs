using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ProgramUpdater;

// ReSharper disable once UnusedType.Global
public class ProgramUpdaterToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProgramUpdaterToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramUpdaterToolCommandFactoryStrategy(ILogger<ProgramUpdaterToolCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectServerTools.ProgramUpdater);

    public async ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //  PublishAndInstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამზადება და პროგრამის დაინსტალირება-განახლება
        //+(CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters=>DownloadPackage=>UpdateProgram=>DownloadParameters=>UpdateParameters)

        ProjectModel projectForUpdate = supportToolsParameters.GetProjectRequired(projectName);

        if (projectForUpdate.IsService)
        {
            var programServiceUpdaterParameters = await ServiceUpdaterParameters.Create(_logger, supportToolsParameters,
                projectName, serverInfo, cancellationToken);
            if (programServiceUpdaterParameters is not null)
            {
                return new ServiceUpdaterToolCommand(_logger, _httpClientFactory, programServiceUpdaterParameters,
                    parametersManager, true);
            }

            StShared.WriteErrorLine("programServiceUpdaterParameters is null", true);
            return null;
        }

        var programUpdaterParameters = await ProgramUpdaterParameters.Create(_logger, supportToolsParameters,
            projectName, serverInfo, cancellationToken);
        if (programUpdaterParameters is not null)
        {
            return new ProgramUpdaterToolCommand(_logger, _httpClientFactory, programUpdaterParameters,
                parametersManager, true);
        }

        StShared.WriteErrorLine("programUpdaterParameters is null", true);
        return null;
    }
}
