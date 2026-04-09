using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ProgramInstaller;

// ReSharper disable once UnusedType.Global
public class ProgramInstallerToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IApplication _app;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProgramInstallerToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramInstallerToolCommandFactoryStrategy(IApplication app,
        ILogger<ProgramInstallerToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _app = app;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectServerTools.ProgramInstaller);

    public async ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //  InstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამოყენებით პროგრამის დაინსტალირება-განახლება
        //+(DownloadPackage=>UpdateProgram=>DownloadParameters=>UpdateParameters)
        var programInstallerParameters = await ProgramInstallerParameters.Create(supportToolsParameters,
            projectName, serverInfo, cancellationToken);

        if (programInstallerParameters is not null)
        {
            return new ProgramInstallerToolCommand(_app.Name, _logger, _httpClientFactory, true,
                programInstallerParameters, parametersManager);
        }

        StShared.WriteErrorLine("programInstallerParameters is null", true);
        return null;
    }
}
