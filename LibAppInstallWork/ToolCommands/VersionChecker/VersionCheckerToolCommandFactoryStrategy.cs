using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.VersionChecker;

// ReSharper disable once UnusedType.Global
public class VersionCheckerToolCommandFactoryStrategy(
    ILogger<VersionCheckerToolCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectServerTools.VersionChecker);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //სერვისის გამაჩერებელი სერვერის მხარეს
        var checkVersionParameters = CheckVersionParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (checkVersionParameters is not null)
        {
            return new ValueTask<IToolCommand?>(new VersionCheckerToolCommand(logger, httpClientFactory,
                checkVersionParameters, parametersManager, true));
        }

        StShared.WriteErrorLine("checkVersionParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
