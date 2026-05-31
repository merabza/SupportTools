using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppInstallWork.ToolActions;
using LibAppInstallWork.ToolCommands.AppSettingsPreparer;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibAppInstallWork.ToolCommands.AppSettingsEncoder;

public sealed class ApplicationSettingsEncoderToolCommand : ToolCommand
{
    private readonly AppSettingsEncoderParameters _appSetEnParameters;
    private readonly AppSettingsPreparerParameters _appSettingsPreparerParameters;
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApplicationSettingsEncoderToolCommand(ILogger logger,
        AppSettingsPreparerParameters appSettingsPreparerParameters, AppSettingsEncoderParameters appSetEnParameters,
        IParametersManager parametersManager) : base(logger, "Encode Settings", appSetEnParameters, parametersManager,
        "Encodes app settings")
    {
        _logger = logger;
        _appSettingsPreparerParameters = appSettingsPreparerParameters;
        _appSetEnParameters = appSetEnParameters;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(_logger,
            _appSetEnParameters.AppSetEnKeysJsonFileName, _appSettingsPreparerParameters.AppSettingsJsonSourceFileName,
            _appSetEnParameters.AppSettingsEncodedJsonFileName, _appSetEnParameters.KeyPart1,
            _appSetEnParameters.KeyPart2, _appSettingsPreparerParameters.ProjectName,
            _appSettingsPreparerParameters.ServerInfo, _appSettingsPreparerParameters.DateMask,
            _appSettingsPreparerParameters.ParametersFileExtension,
            _appSettingsPreparerParameters.FileStorageForExchange, _appSettingsPreparerParameters.ExchangeSmartSchema);
        return await encodeParametersAndUploadAction.Run(cancellationToken);
    }
}
