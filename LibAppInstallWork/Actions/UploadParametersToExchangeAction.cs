﻿using System;
using FileManagersMain;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.Actions;

public sealed class UploadParametersToExchangeAction : ToolAction
{
    private readonly string _dateMask;
    private readonly FileStorageData _exchangeFileStorage;
    private readonly string _parametersContent;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;
    private readonly string _serverName;
    private readonly SmartSchema _uploadSmartSchema;

    public UploadParametersToExchangeAction(ILogger logger, bool useConsole, string projectName, string serverName,
        string dateMask, string parametersFileExtension, string parametersContent, FileStorageData exchangeFileStorage,
        SmartSchema uploadSmartSchema) : base(logger, useConsole, "Upload Parameters To Exchange File Storage")
    {
        _projectName = projectName;
        _serverName = serverName;
        _dateMask = dateMask;
        _parametersFileExtension = parametersFileExtension;
        _parametersContent = parametersContent;
        _exchangeFileStorage = exchangeFileStorage;
        _uploadSmartSchema = uploadSmartSchema;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var serverName = _serverName; //.Capitalize();
        var datePart = DateTime.Now.ToString(_dateMask);
        //ასატვირთი ფაილის სახელის შექმნა
        var uploadFileName = $"{serverName}-{_projectName}-{datePart}{_parametersFileExtension}";

        var exchangeFileManager =
            FileManagersFabric.CreateFileManager(true, Logger, null, _exchangeFileStorage, true);

        if (exchangeFileManager == null)
        {
            Logger.LogError("cannot create file manager"); // for {_exchangeFileStorageName}");
            return false;
        }

        if (!exchangeFileManager.UploadContentToTextFile(_parametersContent, uploadFileName))
        {
            Logger.LogError($"cannot upload parameters content to file {uploadFileName}");
            return false;
        }

        Logger.LogInformation("Remove redundant files...");
        //ზედმეტი ფაილების წაშლა ფაილსერვერის მხარეს
        //პრეფიქსის, სუფიქსისა და ნიღბის გათვალისწინებით ფაილების სიის დადგენა
        //ამ სიიდან ზედმეტი ფაილების დადგენა და წაშლა
        //ზედმეტად ფაილი შეიძლება ჩაითვალოს, თუ ფაილების რაოდენობა მეტი იქნება მაქსიმუმზე
        //წაიშლება უძველესი ფაილები, მანამ სანამ რაოდენობა არ გაუტოლდება მაქსიმუმს
        //SmartSchema? uploadSmartSchema = _smartSchemas.GetSmartSchemaByKey(_uploadSmartSchemaName);
        exchangeFileManager.RemoveRedundantFiles($"{serverName}-{_projectName}-", _dateMask,
            _parametersFileExtension,
            _uploadSmartSchema);

        return true;
    }
}