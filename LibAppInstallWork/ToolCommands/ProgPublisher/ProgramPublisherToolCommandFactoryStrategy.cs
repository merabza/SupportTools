using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ProgPublisher;

// ReSharper disable once UnusedType.Global
public class ProgramPublisherToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<ProgramPublisherToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramPublisherToolCommandFactoryStrategy(ILogger<ProgramPublisherToolCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectServerTools.ProgPublisher);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //  Publish, //პროგრამის საინსტალაციო პაკეტის გამზადება
        //+(CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters)
        var programPublisherParameters =
            ProgramPublisherParameters.Create(_logger, supportToolsParameters, projectName, serverInfo);
        if (programPublisherParameters is null)
        {
            StShared.WriteErrorLine("programPublisherParameters does not created", true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        ProjectModel projectForPublish = supportToolsParameters.GetProjectRequired(projectName);

        if (!projectForPublish.IsService)
        {
            return new ValueTask<IToolCommand?>(new ProgramPublisherToolCommand(_logger, programPublisherParameters,
                parametersManager));
        }

        var appSettingsEncoderParametersForPublish =
            AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);

        if (appSettingsEncoderParametersForPublish != null)
        {
            return new ValueTask<IToolCommand?>(new ServicePublisherToolCommand(_logger, programPublisherParameters,
                appSettingsEncoderParametersForPublish, parametersManager));
        }

        StShared.WriteErrorLine("appSettingsEncoderParametersForPublish does not created", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
