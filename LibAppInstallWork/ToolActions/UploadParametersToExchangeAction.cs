﻿using System;
using System.Threading;
using System.Threading.Tasks;
using FileManagersMain;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

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

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_serverInfo.ServerName))
        {
            _logger.LogError("Server name is not specified");
            return ValueTask.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(_serverInfo.EnvironmentName))
        {
            _logger.LogError("Environment Name is not specified");
            return ValueTask.FromResult(false);
        }

        var datePart = DateTime.Now.ToString(_dateMask);
        var prefix = $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-";
        //ასატვირთი ფაილის სახელის შექმნა
        var uploadFileName = $"{prefix}{datePart}{_parametersFileExtension}";

        var exchangeFileManager =
            FileManagersFactory.CreateFileManager(true, _logger, null, _exchangeFileStorage, true);

        if (exchangeFileManager == null)
        {
            _logger.LogError("cannot create file manager"); // for {_exchangeFileStorageName}");
            return ValueTask.FromResult(false);
        }

        if (!exchangeFileManager.UploadContentToTextFile(_parametersContent, uploadFileName))
        {
            _logger.LogError("cannot upload parameters content to file {uploadFileName}", uploadFileName);
            return ValueTask.FromResult(false);
        }

        _logger.LogInformation("Remove redundant files...");
        //ზედმეტი ფაილების წაშლა ფაილსერვერის მხარეს
        //პრეფიქსის, სუფიქსისა და ნიღბის გათვალისწინებით ფაილების სიის დადგენა
        //ამ სიიდან ზედმეტი ფაილების დადგენა და წაშლა
        //ზედმეტად ფაილი შეიძლება ჩაითვალოს, თუ ფაილების რაოდენობა მეტი იქნება მაქსიმუმზე
        //წაიშლება უძველესი ფაილები, მანამ სანამ რაოდენობა არ გაუტოლდება მაქსიმუმს
        //SmartSchema? uploadSmartSchema = _smartSchemas.GetSmartSchemaByKey(_uploadSmartSchemaName);
        exchangeFileManager.RemoveRedundantFiles(prefix, _dateMask, _parametersFileExtension, _uploadSmartSchema);

        return ValueTask.FromResult(true);
    }
}