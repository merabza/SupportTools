﻿using System;
using System.Collections.Generic;
using System.IO;
using LibAppInstallWork;
using LibAppInstallWork.Actions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SystemToolsShared;

namespace LibAppProjectCreator.JsonCreators;

public sealed class SettingsFilesCreator
{
    private readonly JObject _appSettingsJsonJObject;
    private readonly List<string> _forEncodeAppSettingsJsonKeys;
    private readonly string _keyPart1;
    private readonly ILogger _logger;
    private readonly string _projectFileFullName;
    private readonly string _projectFullPath;
    private readonly JObject _userSecretJsonJObject;

    public SettingsFilesCreator(ILogger logger, string projectFullPath, string projectFileFullName,
        JObject appSettingsJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys, JObject userSecretJsonJObject, string keyPart1)
    {
        _logger = logger;
        _projectFullPath = projectFullPath;
        _projectFileFullName = projectFileFullName;
        _appSettingsJsonJObject = appSettingsJsonJObject;
        _forEncodeAppSettingsJsonKeys = forEncodeAppSettingsJsonKeys;
        _userSecretJsonJObject = userSecretJsonJObject;
        _keyPart1 = keyPart1;
    }

    public bool Run()
    {
        Console.WriteLine("Creating appsettings.json...");
        var sourceJsonFileName =
            Path.Combine(_projectFullPath, "appsettings.json");

        File.WriteAllText(sourceJsonFileName, _appSettingsJsonJObject.ToString());

        if (_forEncodeAppSettingsJsonKeys.Count <= 0)
            return true;

        JArray keysJArray = new();
        foreach (var jsonKey in _forEncodeAppSettingsJsonKeys)
        {
            keysJArray.Add(new JValue(jsonKey));
            if (!StShared.RunProcess(true, _logger, "dotnet",
                    $"user-secrets init --project {_projectFullPath}"))
                return false;
            var userSecretContentFileName =
                UserSecretFileNameDetector.GetFileName(_projectFileFullName);
            if (userSecretContentFileName is null)
                continue;
            //var sf = new FileInfo(userSecretContentFileName);
            //if (sf.DirectoryName is null)
            //    continue;
            //var userSecretDirectoryName = FileStat.CreateFolderIfNotExists(sf.DirectoryName, true, _logger);
            if (FileStat.CreatePrevFolderIfNotExists(userSecretContentFileName, true, _logger))
                File.WriteAllText(userSecretContentFileName, _userSecretJsonJObject.ToString());
        }

        var appSetenKeysJObject = new JObject(new JProperty("Keys", keysJArray));

        Console.WriteLine("Creating appsetenkeys.json...");
        var keysJsonFileName =
            Path.Combine(_projectFullPath, "appsetenkeys.json");
        File.WriteAllText(keysJsonFileName,
            appSetenKeysJObject.ToString(Formatting.Indented));


        Console.WriteLine("Creating appsettingsEncoded.json...");
        var keyPart2 = Environment.MachineName.Capitalize();

        var encodedJsonFileName =
            Path.Combine(_projectFullPath, "appsettingsEncoded.json");
        var encodeParametersAction = new EncodeParametersAction(_logger, keysJsonFileName, sourceJsonFileName,
            encodedJsonFileName, _keyPart1, keyPart2);
        if (encodeParametersAction.Run()) return true;
        _logger.LogError("Cannot encode parameters");
        return false;
    }
}