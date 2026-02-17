using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using ToolsManagement.FileManagersMain;
using ToolsManagement.LibToolActions;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class UploadParametersToExchangeAction : ToolAction
{
    private readonly string _dateMask;
    private readonly FileStorageData _exchangeFileStorage;
    private readonly ILogger _logger;
    private readonly string _parametersContent;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;
    private readonly ServerInfoModel _serverInfo;
    private readonly SmartSchema _uploadSmartSchema;

    public UploadParametersToExchangeAction(ILogger logger, string projectName, ServerInfoModel serverInfo,
        string dateMask, string parametersFileExtension, string parametersContent, FileStorageData exchangeFileStorage,
        SmartSchema uploadSmartSchema) : base(logger, "Upload Parameters To Exchange File Storage", null, null)
    {
        _logger = logger;
        _projectName = projectName;
        _serverInfo = serverInfo;
        _dateMask = dateMask;
        _parametersFileExtension = parametersFileExtension;
        _parametersContent = parametersContent;
        _exchangeFileStorage = exchangeFileStorage;
        _uploadSmartSchema = uploadSmartSchema;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_serverInfo.ServerName))
        {
            _logger.LogError("Server name is not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_serverInfo.EnvironmentName))
        {
            _logger.LogError("Environment Name is not specified");
            return false;
        }

        string datePart = DateTime.Now.ToString(_dateMask, CultureInfo.InvariantCulture);
        string prefix = $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-";
        //ასატვირთი ფაილის სახელის შექმნა
        string uploadFileName = $"{prefix}{datePart}{_parametersFileExtension}";

        FileManager? exchangeFileManager =
            FileManagersFactory.CreateFileManager(true, _logger, null, _exchangeFileStorage, true);

        if (exchangeFileManager == null)
        {
            _logger.LogError("cannot create file manager"); // for {_exchangeFileStorageName}");
            return false;
        }

        if (!await exchangeFileManager.UploadContentToTextFileAsync(_parametersContent, uploadFileName,
                cancellationToken))
        {
            _logger.LogError("cannot upload parameters content to file {UploadFileName}", uploadFileName);
            return false;
        }

        _logger.LogInformation("Remove redundant files...");
        //ზედმეტი ფაილების წაშლა ფაილსერვერის მხარეს
        //პრეფიქსის, სუფიქსისა და ნიღბის გათვალისწინებით ფაილების სიის დადგენა
        //ამ სიიდან ზედმეტი ფაილების დადგენა და წაშლა
        //ზედმეტად ფაილი შეიძლება ჩაითვალოს, თუ ფაილების რაოდენობა მეტი იქნება მაქსიმუმზე
        //წაიშლება უძველესი ფაილები, მანამ სანამ რაოდენობა არ გაუტოლდება მაქსიმუმს
        //SmartSchema? uploadSmartSchema = _smartSchemas.GetSmartSchemaByKey(_uploadSmartSchemaName);
        exchangeFileManager.RemoveRedundantFiles(prefix, _dateMask, _parametersFileExtension, _uploadSmartSchema);

        return true;
    }
}
