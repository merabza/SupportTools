using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;
using ToolsManagement.LibToolActions;

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

        if (string.IsNullOrWhiteSpace(_mainProjectFileName))
        {
            _logger.LogError("Project file name is not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_projectName))
        {
            _logger.LogError("Project name is not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_workFolder))
        {
            _logger.LogError("Work Folder name is not specified");
            return false;
        }

        //თუ არ არსებობს, შეიქმნას სამუშაო ფოლდერი
        if (!StShared.CreateFolder(_workFolder, true))
        {
            return false;
        }

        const string archiveFileExtension = ".zip";

        string datePart = DateTime.Now.ToString(_dateMask, CultureInfo.InvariantCulture);
        //სახელის შექმნა output ფოლდერისათვის
        string outputFolderName =
            $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-{_runtime}-{datePart}";
        string outputFolderPath = Path.Combine(_workFolder, outputFolderName);
        //სახელის შექმნა ZIP ფაილისათვის
        string zipFileName =
            $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-{_runtime}-{datePart}{archiveFileExtension}";
        string zipFileFullName = Path.Combine(_workFolder, zipFileName);

        //შევამოწმოთ შემთხვევით ხომ არ არსებობს output ფოლდერი
        //თუ არსებობს გამოვიტანოთ შეცდომა და დავასრულოთ პროცესი
        if (Directory.Exists(outputFolderPath))
        {
            StShared.WriteErrorLine($"Project output folder {outputFolderPath} is already exists", true, _logger);
            return false;
        }

        //თუ არ არსებობს, შევქმნათ
        if (!StShared.CreateFolder(outputFolderPath, true))
        {
            return false;
        }

        _logger.LogInformation("Detecting version...");

        XElement projectXml = XElement.Load(_mainProjectFileName);

        XElement? xmlVersionId = projectXml.Descendants("PropertyGroup").Descendants("Version").SingleOrDefault();

        string? versionId = null;
        if (xmlVersionId != null)
        {
            versionId = xmlVersionId.Value;
        }

        //თუ ასეთი მნიშვნელობის ამოღება მოხერხდა, მაშინ
        versionId ??= "1.0.0";

        string[] verNumbers = versionId.Split('.');
        var assemblyVersionNumbers = new List<string>();
        assemblyVersionNumbers.AddRange(verNumbers.Take(2));
        if (assemblyVersionNumbers.Count == 0)
        {
            assemblyVersionNumbers.Add("1");
        }

        if (assemblyVersionNumbers.Count == 1)
        {
            assemblyVersionNumbers.Add("0");
        }

        DateTime todayDate = DateTime.Today;
        DateTime now = DateTime.Now;

        assemblyVersionNumbers.Add(
            (todayDate - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays.ToString(CultureInfo
                .InvariantCulture));
        assemblyVersionNumbers.Add(((int)(now - todayDate).TotalSeconds / 2).ToString(CultureInfo.InvariantCulture));
        AssemblyVersion = string.Join('.', assemblyVersionNumbers);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("dotnet publishing with assemblyVersion {AssemblyVersion} ...", AssemblyVersion);
        }

        var dotnetProcessor = new DotnetProcessor(_logger, true);

        //მთავარი პროექტის შექმნა
        if (dotnetProcessor.PublishRelease(_runtime, outputFolderPath, _mainProjectFileName, AssemblyVersion).IsSome)
        {
            _logger.LogError("Cannot publish project {_projectName}", _projectName);
            return false;
        }

        _logger.LogInformation("Delete Redundant Files");
        //outputFolderPath ფოლდერიდან წაიშალოს ზედმეტი ფაილები
        foreach (string? fileName in _redundantFileNames.Select(Path.GetFileName))
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var di = new DirectoryInfo(outputFolderPath);
            foreach (FileInfo file in di.GetFiles(fileName))
            {
                string fName = file.Name;
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Delete {FileName}", fName);
                }

                file.Delete();
            }
        }

        //}
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Archiving {ZipFileFullName}...", zipFileFullName);
        }

        //შექმნილი output ფოლდერიდან ZIP ფაილის შექმნა, იმავე სამუშაო ფოლდერში
        await ZipFile.CreateFromDirectoryAsync(outputFolderPath, zipFileFullName, CompressionLevel.Optimal, false,
            cancellationToken);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Removing folder {OutputFolderPath}...", outputFolderPath);
        }

        //output ფოლდერის წაშლა
        Directory.Delete(outputFolderPath, true);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Uploading {ZipFileFullName}...", zipFileFullName);
        }

        //ZIP ფაილის ატვირთვა FTP-ზე
        FileManager? exchangeFileManager =
            FileManagersFactory.CreateFileManager(true, _logger, _workFolder, _exchangeFileStorage);

        if (exchangeFileManager == null)
        {
            _logger.LogError("cannot create file manager");
            return false;
        }

        if (!exchangeFileManager.UploadFile(zipFileName, _uploadTempExtension))
        {
            _logger.LogError("cannot upload file {ZipFileName}", zipFileName);
            return false;
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
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Deleting {ZipFileFullName} file...", zipFileFullName);
        }

        //წაიშალოს ლოკალური ფაილი
        File.Delete(zipFileFullName);

        FileManager? workFileManager = FileManagersFactory.CreateFileManager(true, _logger, _workFolder);

        workFileManager?.RemoveRedundantFiles(
            $"{_serverInfo.ServerName}-{_serverInfo.EnvironmentName}-{_projectName}-{_runtime}-", _dateMask,
            archiveFileExtension, _smartSchemaForLocal);

        return true;
    }
}
