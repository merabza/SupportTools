using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.Actions;

public sealed class EncodeParametersAndUploadAction : ToolAction
{
    private readonly string _dateMask;
    private readonly string _encodedJsonFileName;
    private readonly FileStorageData _exchangeFileStorage;
    private readonly string _keyPart1;
    private readonly string _keyPart2;
    private readonly string _keysJsonFileName;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;
    private readonly string _serverName;
    private readonly string _sourceJsonFileName;
    private readonly SmartSchema _uploadSmartSchema;

    public EncodeParametersAndUploadAction(ILogger logger, bool useConsole, string keysJsonFileName,
        string sourceJsonFileName, string encodedJsonFileName, string keyPart1, string keyPart2, string projectName,
        string serverName, string dateMask, string parametersFileExtension, FileStorageData exchangeFileStorage,
        SmartSchema uploadSmartSchema) : base(logger, useConsole, "Encode Parameters And Upload")
    {
        _keysJsonFileName = keysJsonFileName;
        _sourceJsonFileName = sourceJsonFileName;
        _encodedJsonFileName = encodedJsonFileName;
        _keyPart1 = keyPart1;
        _keyPart2 = keyPart2;
        _projectName = projectName;
        _serverName = serverName;
        _dateMask = dateMask;
        _parametersFileExtension = parametersFileExtension;
        _uploadSmartSchema = uploadSmartSchema;
        _exchangeFileStorage = exchangeFileStorage;
    }

    public string? AppSettingsVersion { get; private set; }

    public string? EncodedJsonContent { get; private set; }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var encodeParametersAction = new EncodeParametersAction(Logger, UseConsole, _keysJsonFileName,
            _sourceJsonFileName, _encodedJsonFileName, _keyPart1, _keyPart2);
        if (!encodeParametersAction.Run())
        {
            Logger.LogError("Cannot encode parameters");
            return false;
        }

        AppSettingsVersion = encodeParametersAction.AppSettingsVersion;
        EncodedJsonContent = encodeParametersAction.EncodedJsonContent;

        if (EncodedJsonContent is null)
        {
            Logger.LogError("Encoded Parameters Json Content is null");
            return false;
        }


        var uploadParametersToExchangeAction = new UploadParametersToExchangeAction(Logger, UseConsole, _projectName,
            _serverName, _dateMask, _parametersFileExtension, EncodedJsonContent, _exchangeFileStorage,
            _uploadSmartSchema);
        return uploadParametersToExchangeAction.Run();
    }
}