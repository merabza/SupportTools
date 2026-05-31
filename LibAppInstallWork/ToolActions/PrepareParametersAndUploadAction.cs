using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class PrepareParametersAndUploadAction : ToolAction
{
    private readonly string _dateMask;
    private readonly FileStorageData _exchangeFileStorage;
    private readonly ILogger _logger;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;
    private readonly ServerInfoModel _serverInfo;
    private readonly string _sourceJsonFileName;
    private readonly SmartSchema _uploadSmartSchema;

    public PrepareParametersAndUploadAction(ILogger logger, string sourceJsonFileName, string projectName,
        ServerInfoModel serverInfo, string dateMask, string parametersFileExtension,
        FileStorageData exchangeFileStorage, SmartSchema uploadSmartSchema) : base(logger,
        "Prepare Parameters And Upload", null, null)
    {
        _logger = logger;
        _sourceJsonFileName = sourceJsonFileName;
        _projectName = projectName;
        _serverInfo = serverInfo;
        _dateMask = dateMask;
        _parametersFileExtension = parametersFileExtension;
        _uploadSmartSchema = uploadSmartSchema;
        _exchangeFileStorage = exchangeFileStorage;
    }

    public string? AppSettingsVersion { get; private set; }

    public string? PreparedJsonContent { get; private set; }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var prepareParametersAction = new PrepareAppSettingsParametersAction(_logger, _sourceJsonFileName);
        if (!await prepareParametersAction.Run(cancellationToken))
        {
            _logger.LogError("Cannot Prepare parameters");
            return false;
        }

        AppSettingsVersion = prepareParametersAction.AppSettingsVersion;
        PreparedJsonContent = prepareParametersAction.PreparedAppSettingsJsonContent;

        if (PreparedJsonContent is null)
        {
            _logger.LogError("Prepared Parameters Json Content is null");
            return false;
        }

        var uploadParametersToExchangeAction = new UploadParametersToExchangeAction(_logger, _projectName, _serverInfo,
            _dateMask, _parametersFileExtension, PreparedJsonContent, _exchangeFileStorage, _uploadSmartSchema);
        return await uploadParametersToExchangeAction.Run(cancellationToken);
    }
}
