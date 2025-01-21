using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ApplicationSettingsEncoder : ToolCommand
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApplicationSettingsEncoder(ILogger logger, AppSettingsEncoderParameters appSetEnParameters,
        IParametersManager parametersManager) : base(logger, "Encode Settings", appSetEnParameters, parametersManager,
        "Encodes app settings")
    {
        _logger = logger;
    }

    private AppSettingsEncoderParameters AppSetEnParameters => (AppSettingsEncoderParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(_logger,
            AppSetEnParameters.AppSetEnKeysJsonFileName, AppSetEnParameters.AppSettingsJsonSourceFileName,
            AppSetEnParameters.AppSettingsEncodedJsonFileName, AppSetEnParameters.KeyPart1, AppSetEnParameters.KeyPart2,
            AppSetEnParameters.ProjectName, AppSetEnParameters.ServerInfo, AppSetEnParameters.DateMask,
            AppSetEnParameters.ParametersFileExtension, AppSetEnParameters.FileStorageForExchange,
            AppSetEnParameters.ExchangeSmartSchema);
        return await encodeParametersAndUploadAction.Run(cancellationToken);
    }
}