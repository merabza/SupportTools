using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppInstallWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibAppInstallWork.ToolCommands.AppSettingsPreparer;

public sealed class ApplicationSettingsPreparerToolCommand : ToolCommand
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApplicationSettingsPreparerToolCommand(ILogger logger, AppSettingsPreparerParameters appSetEnParameters,
        IParametersManager parametersManager) : base(logger, "Prepare Settings", appSetEnParameters, parametersManager,
        "Prepares app settings")
    {
        _logger = logger;
    }

    private AppSettingsPreparerParameters AppSetEnParameters => (AppSettingsPreparerParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var encodeParametersAndUploadAction = new PrepareParametersAndUploadAction(_logger,
            AppSetEnParameters.AppSettingsJsonSourceFileName, AppSetEnParameters.ProjectName,
            AppSetEnParameters.ServerInfo, AppSetEnParameters.DateMask, AppSetEnParameters.ParametersFileExtension,
            AppSetEnParameters.FileStorageForExchange, AppSetEnParameters.ExchangeSmartSchema);
        return await encodeParametersAndUploadAction.Run(cancellationToken);
    }
}
