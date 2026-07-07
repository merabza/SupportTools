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
public class ProgramUpdaterToolCommandFactoryStrategy(
    IApplication app,
    ILogger<ProgramUpdaterToolCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory) : IToolCommandFactoryStrategy
{
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

        if (projectForUpdate.ProjectType == EProjectType.IsService)
        {
            var programServiceUpdaterParameters = await ServiceUpdaterParameters.Create(logger, supportToolsParameters,
                projectName, serverInfo, cancellationToken);
            if (programServiceUpdaterParameters is not null)
            {
                return new ServiceUpdaterToolCommand(app.AppName, logger, httpClientFactory,
                    programServiceUpdaterParameters, parametersManager, true);
            }

            StShared.WriteErrorLine("programServiceUpdaterParameters is null", true);
            return null;
        }

        var programUpdaterParameters = await ProgramUpdaterParameters.Create(logger, supportToolsParameters,
            projectName, serverInfo, cancellationToken);
        if (programUpdaterParameters is not null)
        {
            return new ProgramUpdaterToolCommand(app.AppName, logger, httpClientFactory, programUpdaterParameters,
                parametersManager, true);
        }

        StShared.WriteErrorLine("programUpdaterParameters is null", true);
        return null;
    }
}
