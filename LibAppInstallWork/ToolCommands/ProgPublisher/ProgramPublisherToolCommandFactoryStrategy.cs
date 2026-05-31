using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using LibAppInstallWork.ToolCommands.AppSettingsPreparer;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ProgPublisher;

// ReSharper disable once UnusedType.Global
public class ProgramPublisherToolCommandFactoryStrategy(ILogger<ProgramPublisherToolCommandFactoryStrategy> logger)
    : IToolCommandFactoryStrategy
{
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
            ProgramPublisherParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
        if (programPublisherParameters is null)
        {
            StShared.WriteErrorLine("programPublisherParameters does not created", true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        ProjectModel projectForPublish = supportToolsParameters.GetProjectRequired(projectName);

        if (!projectForPublish.IsService)
        {
            return new ValueTask<IToolCommand?>(new ProgramPublisherToolCommand(logger, programPublisherParameters,
                parametersManager));
        }

        var appSettingsPreparerParameters =
            AppSettingsPreparerParameters.Create(supportToolsParameters, projectName, serverInfo);

        if (appSettingsPreparerParameters is null)
        {
            StShared.WriteErrorLine("appSettingsEncoderParametersForPublish does not created", true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        var appSettingsEncoderParametersForPublish =
            AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);

        return new ValueTask<IToolCommand?>(new ServicePublisherToolCommand(logger, programPublisherParameters,
            appSettingsPreparerParameters, appSettingsEncoderParametersForPublish, parametersManager));
    }
}
