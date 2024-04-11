using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ApplicationSettingsEncoder : ToolCommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ApplicationSettingsEncoder(ILogger logger, AppSettingsEncoderParameters appsetenParameters,
        IParametersManager parametersManager) : base(logger, "Encode Settings", appsetenParameters, parametersManager)
    {
    }

    private AppSettingsEncoderParameters AppsetenParameters => (AppSettingsEncoderParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(Logger,
            AppsetenParameters.AppSetEnKeysJsonFileName, AppsetenParameters.AppSettingsJsonSourceFileName,
            AppsetenParameters.AppSettingsEncodedJsonFileName, AppsetenParameters.KeyPart1, AppsetenParameters.KeyPart2,
            AppsetenParameters.ProjectName, AppsetenParameters.ServerInfo, AppsetenParameters.DateMask,
            AppsetenParameters.ParametersFileExtension, AppsetenParameters.FileStorageForExchange,
            AppsetenParameters.ExchangeSmartSchema);
        return await encodeParametersAndUploadAction.Run(cancellationToken);
    }
}