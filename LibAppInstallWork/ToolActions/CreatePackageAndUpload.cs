using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FileManagersMain;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class CreatePackageAndUpload : ToolAction
{
    private readonly string _dateMask;
    private readonly FileStorageData _exchangeFileStorage;
    private readonly ILogger _logger;
    private readonly string _mainProjectFileName;
    private readonly string _projectName;
    private readonly List<string> _redundantFileNames;
    private readonly string _runtime;
    private readonly ServerInfoModel _serverInfo;
    private readonly SmartSchema _smartSchemaForLocal;
    private readonly SmartSchema _uploadSmartSchema;
    private readonly string _uploadTempExtension;
    private readonly string _workFolder;

    public CreatePackageAndUpload(ILogger logger, string projectName, string mainProjectFileName,
        ServerInfoModel serverInfo, string workFolder, string dateMask, string runtime, List<string> redundantFileNames,
        string uploadTempExtension, FileStorageData exchangeFileStorage, SmartSchema smartSchemaForLocal,
        SmartSchema uploadSmartSchema) : base(logger, "Program Publisher", null, null)
    {
        _logger = logger;
        _projectName = projectName;
        _mainProjectFileName = mainProjectFileName;
        _serverInfo = serverInfo;
        _workFolder = workFolder;
        _dateMask = dateMask;
        _runtime = runtime;
        _redundantFileNames = redundantFileNames;
        _uploadTempExtension = uploadTempExtension;
        _smartSchemaForLocal = smartSchemaForLocal;
        _uploadSmartSchema = uploadSmartSchema;
        _exchangeFileStorage = exchangeFileStorage;
    }

    public string? AssemblyVersion { get; private set; }

    protected override Task<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_serverInfo.ServerName))
        {
            _logger.LogError("Server name is not specified");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(_serverInfo.EnvironmentName))
        {
            _logger.LogError("Environment Name is not specified");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(_mainProjectFileName))
        {
            _logger.LogError("Project file name is not specified");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(_projectName))
        {
            _logger.LogError("Project name is not specified");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(_workFolder))
        {
            _logger.LogError("Work Folder name is not specified");
            return Task.FromResult(false);
        }

        //თუ არ არსებობს, შეიქმნას სამუშაო ფოლდერი
        if (!StShared.CreateFolder(_workFolder, true))
            return Task.FromResult(false);

        const string archiveFileExtension = ".zip";

        var datePart = DateTime.Now.ToString(_dateMask);
        //სახელის შექმნა output ფოლდერისათვის
        var outputFolderName =
            $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-{_runtime}-{datePart}";
        var outputFolderPath = Path.Combine(_workFolder, outputFolderName);
        //სახელის შექმნა ZIP ფაილისათვის
        var zipFileName =
            $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-{_runtime}-{datePart}{archiveFileExtension}";
        var zipFileFullName = Path.Combine(_workFolder, zipFileName);

        //შევამოწმოთ შემთხვევით ხომ არ არსებობს output ფოლდერი
        //თუ არსებობს გამოვიტანოთ შეცდომა და დავასრულოთ პროცესი
        if (Directory.Exists(outputFolderPath))
        {
            StShared.WriteErrorLine($"Project output folder {outputFolderPath} is already exists", true, _logger);
            return Task.FromResult(false);
        }

        //თუ არ არსებობს, შევქმნათ
        if (!StShared.CreateFolder(outputFolderPath, true))
            return Task.FromResult(false);

        _logger.LogInformation("Detecting version...");

        var projectXml = XElement.Load(_mainProjectFileName);

        var xmlVersionId = projectXml.Descendants("PropertyGroup").Descendants("Version").SingleOrDefault();

        string? versionId = null;
        if (xmlVersionId != null)
            versionId = xmlVersionId.Value;

        //თუ ასეთი მნიშვნელობის ამოღება მოხერხდა, მაშინ
        versionId ??= "1.0.0";

        var verNumbers = versionId.Split('.');
        var assemblyVersionNumbers = new List<string>();
        assemblyVersionNumbers.AddRange(verNumbers.Take(2));
        if (assemblyVersionNumbers.Count == 0)
            assemblyVersionNumbers.Add("1");
        if (assemblyVersionNumbers.Count == 1)
            assemblyVersionNumbers.Add("0");

        var todayDate = DateTime.Today;

        assemblyVersionNumbers.Add(
            (todayDate - new DateTime(2000, 1, 1)).TotalDays.ToString(CultureInfo.InvariantCulture));
        assemblyVersionNumbers.Add(
            ((int)(DateTime.Now - todayDate).TotalSeconds / 2).ToString(CultureInfo.InvariantCulture));
        AssemblyVersion = string.Join('.', assemblyVersionNumbers);

        _logger.LogInformation("dotnet publishing with assemblyVersion {AssemblyVersion} ...", AssemblyVersion);

        //მთავარი პროექტის შექმნა
        if (StShared.RunProcess(true, _logger, "dotnet",
                $"publish --configuration Release --runtime {_runtime} --self-contained --output {outputFolderPath} {_mainProjectFileName} /p:AssemblyVersion={AssemblyVersion}")
            .IsSome)
        {
            _logger.LogError("Cannot publish project {_projectName}", _projectName);
            return Task.FromResult(false);
        }

        //if (_redundantFileNames != null)
        //{
        _logger.LogInformation("Delete Redundant Files");
        //outputFolderPath ფოლდერიდან წაიშალოს ზედმეტი ფაილები
        foreach (var fileName in _redundantFileNames.Select(Path.GetFileName))
        {
            if (string.IsNullOrWhiteSpace(fileName))
                continue;
            var di = new DirectoryInfo(outputFolderPath);
            foreach (var file in di.GetFiles(fileName))
            {
                var fName = file.Name;
                _logger.LogInformation("Delete {fName}", fName);
                file.Delete();
            }
        }
        //}


        _logger.LogInformation("Archiving {zipFileFullName}...", zipFileFullName);
        //შექმნილი output ფოლდერიდან ZIP ფაილის შექმნა, იმავე სამუშაო ფოლდერში
        ZipFile.CreateFromDirectory(outputFolderPath, zipFileFullName, CompressionLevel.Optimal, false);

        _logger.LogInformation("Removing folder {outputFolderPath}...", outputFolderPath);
        //output ფოლდერის წაშლა
        Directory.Delete(outputFolderPath, true);

        _logger.LogInformation("Uploading {zipFileFullName}...", zipFileFullName);
        //ZIP ფაილის ატვირთვა FTP-ზე
        var exchangeFileManager =
            FileManagersFabric.CreateFileManager(true, _logger, _workFolder, _exchangeFileStorage);

        if (exchangeFileManager == null)
        {
            _logger.LogError("cannot create file manager");
            return Task.FromResult(false);
        }

        if (!exchangeFileManager.UploadFile(zipFileName, _uploadTempExtension))
        {
            _logger.LogError("cannot upload file {zipFileName}", zipFileName);
            return Task.FromResult(false);
        }

        _logger.LogInformation("Remove redundant files...");

        //SmartSchema? localSmartSchema = _smartSchemas.GetSmartSchemaByKey(_localSmartSchemaName);
        //SmartSchema? uploadSmartSchema = _smartSchemas.GetSmartSchemaByKey(_uploadSmartSchemaName);

        //ზედმეტი ფაილების წაშლა ფაილსერვერის მხარეს
        //პრეფიქსის, სუფიქსისა და ნიღბის გათვალისწინებით ფაილების სიის დადგენა
        //ამ სიიდან ზედმეტი ფაილების დადგენა და წაშლა
        //ზედმეტად ფაილი შეიძლება ჩაითვალოს, თუ ფაილების რაოდენობა მეტი იქნება მაქსიმუმზე
        //წაიშლება უძველესი ფაილები, მანამ სანამ რაოდენობა არ გაუტოლდება მაქსიმუმს
        exchangeFileManager.RemoveRedundantFiles(
            $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-{_runtime}-", _dateMask,
            archiveFileExtension, _uploadSmartSchema);

        _logger.LogInformation("Deleting {zipFileFullName} file...", zipFileFullName);
        //წაიშალოს ლოკალური ფაილი
        File.Delete(zipFileFullName);


        var workFileManager = FileManagersFabric.CreateFileManager(true, _logger, _workFolder);

        workFileManager?.RemoveRedundantFiles(
            $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-{_runtime}-", _dateMask,
            archiveFileExtension, _smartSchemaForLocal);

        return Task.FromResult(true);
    }
}