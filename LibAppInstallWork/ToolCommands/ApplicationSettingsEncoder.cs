using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ApplicationSettingsEncoder : ToolCommand
{
    public ApplicationSettingsEncoder(ILogger logger, bool useConsole, AppSettingsEncoderParameters appsetenParameters,
        IParametersManager parametersManager) : base(logger, useConsole, "Encode Settings", appsetenParameters,
        parametersManager)
    {
    }

    private AppSettingsEncoderParameters AppsetenParameters => (AppSettingsEncoderParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(Logger, UseConsole,
            AppsetenParameters.AppSetEnKeysJsonFileName, AppsetenParameters.AppSettingsJsonSourceFileName,
            AppsetenParameters.AppSettingsEncodedJsonFileName, AppsetenParameters.KeyPart1, AppsetenParameters.KeyPart2,
            AppsetenParameters.ProjectName, AppsetenParameters.ServerInfo, AppsetenParameters.DateMask,
            AppsetenParameters.ParametersFileExtension, AppsetenParameters.FileStorageForExchange,
            AppsetenParameters.ExchangeSmartSchema);
        return encodeParametersAndUploadAction.Run();
    }
}