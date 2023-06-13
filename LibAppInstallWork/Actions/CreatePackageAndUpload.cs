using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using FileManagersMain;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppInstallWork.Actions;

public sealed class CreatePackageAndUpload : ToolAction
{
    private readonly string _dateMask;
    private readonly FileStorageData _exchangeFileStorage;
    private readonly string _mainProjectFileName;
    private readonly string _projectName;
    private readonly List<string> _redundantFileNames;
    private readonly string _runtime;
    private readonly string _serverName;
    private readonly SmartSchema _smartSchemaForLocal;
    private readonly SmartSchema _uploadSmartSchema;
    private readonly string _uploadTempExtension;
    private readonly string _workFolder;

    public CreatePackageAndUpload(ILogger logger, bool useConsole, string projectName, string mainProjectFileName,
        string serverName, string workFolder, string dateMask, string runtime, List<string> redundantFileNames,
        string uploadTempExtension, FileStorageData exchangeFileStorage, SmartSchema smartSchemaForLocal,
        SmartSchema uploadSmartSchema) : base(logger, useConsole, "Program Publisher")
    {
        _projectName = projectName;
        _mainProjectFileName = mainProjectFileName;
        _serverName = serverName;
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

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        if (string.IsNullOrWhiteSpace(_mainProjectFileName))
        {
            Logger.LogError("Project file name not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_projectName))
        {
            Logger.LogError("Project name not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_workFolder))
        {
            Logger.LogError("Work Folder name not specified");
            return false;
        }

        //თუ არ არსებობს, შეიქმნას სამუშაო ფოლდერი
        if (!StShared.CreateFolder(_workFolder, true))
            return false;

        var archiveFileExtension = ".zip";

        var datePart = DateTime.Now.ToString(_dateMask);
        //სახელის შექმნა output ფოლდერისათვის
        var outputFolderName = $"{_serverName}-{_projectName}-{_runtime}-{datePart}";
        var outputFolderPath = Path.Combine(_workFolder, outputFolderName);
        //სახელის შექმნა ZIP ფაილისათვის
        var zipFileName = $"{_serverName}-{_projectName}-{_runtime}-{datePart}{archiveFileExtension}";
        var zipFileFullName = Path.Combine(_workFolder, zipFileName);

        //შევამოწმოთ შემთხვევით ხომ არ არსებობს output ფოლდერი
        //თუ არსებობს გამოვიტანოთ შეცდომა და დავასრულოთ პროცესი
        if (Directory.Exists(outputFolderPath))
        {
            StShared.WriteErrorLine($"Project output folder {outputFolderPath} is already exists", true, Logger);
            return false;
        }

        //თუ არ არსებობს, შევქმნათ
        if (!StShared.CreateFolder(outputFolderPath, true))
            return false;

        Logger.LogInformation("Detecting version...");

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

        Logger.LogInformation($"dotnet publishing with assemblyVersion {AssemblyVersion} ...");

        //მთავარი პროექტის შექმნა
        if (!StShared.RunProcess(true, Logger, "dotnet",
                $"publish --configuration Release --runtime {_runtime} --self-contained --output {outputFolderPath} {_mainProjectFileName} /p:AssemblyVersion={AssemblyVersion}"))
        {
            Logger.LogError($"Cannot publish project {_projectName}");
            return false;
        }

        //if (_redundantFileNames != null)
        //{
        Logger.LogInformation("Delete Redundant Files");
        //outputFolderPath ფოლდერიდან წაიშალოს ზედმეტი ფაილები
        foreach (var fileName in _redundantFileNames.Select(Path.GetFileName))
        {
            if (string.IsNullOrWhiteSpace(fileName))
                continue;
            var di = new DirectoryInfo(outputFolderPath);
            foreach (var file in di.GetFiles(fileName))
            {
                Logger.LogInformation($"Delete {file.Name}");
                file.Delete();
            }
        }
        //}


        Logger.LogInformation($"Archiving {zipFileFullName}...");
        //შექმნილი output ფოლდერიდან ZIP ფაილის შექმნა, იმავე სამუშაო ფოლდერში
        ZipFile.CreateFromDirectory(outputFolderPath, zipFileFullName, CompressionLevel.Optimal, false);

        Logger.LogInformation($"Removing folder {outputFolderPath}...");
        //output ფოლდერის წაშლა
        Directory.Delete(outputFolderPath, true);

        Logger.LogInformation($"Uploading {zipFileFullName}...");
        //ZIP ფაილის ატვირთვა FTP-ზე
        var exchangeFileManager =
            FileManagersFabric.CreateFileManager(true, Logger, _workFolder, _exchangeFileStorage);

        if (exchangeFileManager == null)
        {
            Logger.LogError("cannot create file manager");
            return false;
        }

        if (!exchangeFileManager.UploadFile(zipFileName, _uploadTempExtension))
        {
            Logger.LogError($"cannot upload file {zipFileName}");
            return false;
        }

        Logger.LogInformation("Remove redundant files...");

        //SmartSchema? localSmartSchema = _smartSchemas.GetSmartSchemaByKey(_localSmartSchemaName);
        //SmartSchema? uploadSmartSchema = _smartSchemas.GetSmartSchemaByKey(_uploadSmartSchemaName);

        //ზედმეტი ფაილების წაშლა ფაილსერვერის მხარეს
        //პრეფიქსის, სუფიქსისა და ნიღბის გათვალისწინებით ფაილების სიის დადგენა
        //ამ სიიდან ზედმეტი ფაილების დადგენა და წაშლა
        //ზედმეტად ფაილი შეიძლება ჩაითვალოს, თუ ფაილების რაოდენობა მეტი იქნება მაქსიმუმზე
        //წაიშლება უძველესი ფაილები, მანამ სანამ რაოდენობა არ გაუტოლდება მაქსიმუმს
        exchangeFileManager.RemoveRedundantFiles($"{_serverName}-{_projectName}-{_runtime}-", _dateMask,
            archiveFileExtension, _uploadSmartSchema);

        Logger.LogInformation($"Deleting {zipFileFullName} file...");
        //წაიშალოს ლოკალური ფაილი
        File.Delete(zipFileFullName);


        var workFileManager = FileManagersFabric.CreateFileManager(true, Logger, _workFolder);

        workFileManager?.RemoveRedundantFiles($"{_serverName}-{_projectName}-{_runtime}-", _dateMask,
            archiveFileExtension, _smartSchemaForLocal);

        return true;
    }
}