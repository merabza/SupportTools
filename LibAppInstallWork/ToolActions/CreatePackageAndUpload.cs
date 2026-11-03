using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppCliTools.LibDataInput;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class CreatePackageAndUpload : ToolAction
{
    private readonly Func<string, bool> _confirm;
    private readonly string _dateMask;
    private readonly FileStorageData _exchangeFileStorage;
    private readonly ILogger _logger;
    private readonly string _mainProjectFileName;
    private readonly string _projectName;
    private readonly Func<string, string, string, string, bool> _publishRelease;
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
        SmartSchema uploadSmartSchema) : this(logger, projectName, mainProjectFileName, serverInfo, workFolder,
        dateMask, runtime, redundantFileNames, uploadTempExtension, exchangeFileStorage, smartSchemaForLocal,
        uploadSmartSchema, question => Inputer.InputBool(question, false, false),
        (runtime, outputFolderPath, projectFileName, assemblyVersion) => new DotnetProcessor(logger, true)
            .PublishRelease(runtime, outputFolderPath, projectFileName, assemblyVersion).IsSuccess)
    {
    }

    //მომხმარებლის კითხვა და dotnet publish პარამეტრადაა გამოტანილი, რომ ტესტებმა კონსოლი არ წაიკითხონ და dotnet არ გაუშვან
    internal CreatePackageAndUpload(ILogger logger, string projectName, string mainProjectFileName,
        ServerInfoModel serverInfo, string workFolder, string dateMask, string runtime, List<string> redundantFileNames,
        string uploadTempExtension, FileStorageData exchangeFileStorage, SmartSchema smartSchemaForLocal,
        SmartSchema uploadSmartSchema, Func<string, bool> confirm,
        Func<string, string, string, string, bool> publishRelease) : base(logger, "Program Publisher", null, null)
    {
        _confirm = confirm;
        _publishRelease = publishRelease;
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

        _logger.LogInformation("Cleaning previous build outputs...");

        //წინა ბილდების ნაშთები იწმინდება, რომ პაკეტში ძველ რეფერენსებზე აწყობილი assembly-ები არ მოხვდეს.
        //dotnet clean აქ არ გამოდგება, რადგან ის მოითხოვს შესაბამისი runtime-ით გაკეთებულ restore-ს (NETSDK1047),
        //ამიტომ bin და obj ფოლდერები პირდაპირ იშლება. publish თვითონ გააკეთებს ახალ restore-ს.
        string? mainProjectFolderPath = Path.GetDirectoryName(_mainProjectFileName);
        if (string.IsNullOrWhiteSpace(mainProjectFolderPath))
        {
            _logger.LogError("Cannot detect project folder for {_mainProjectFileName}", _mainProjectFileName);
            return false;
        }

        if (!StopProgramRunningFromBin(mainProjectFolderPath))
        {
            return false;
        }

        FileStat.DeleteDirectoryIfExists(Path.Combine(mainProjectFolderPath, "bin"));
        FileStat.DeleteDirectoryIfExists(Path.Combine(mainProjectFolderPath, "obj"));

        //მთავარი პროექტის შექმნა
        if (!_publishRelease(_runtime, outputFolderPath, _mainProjectFileName, AssemblyVersion))
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

    //თუ პროგრამა გაშვებულია პროექტის bin ფოლდერიდან (მაგალითად, დეველოპმენტისას), მისი ფაილები დაბლოკილია
    //და bin ფოლდერის წაშლა ვერ მოხერხდება. ასეთ შემთხვევაში მომხმარებელს ვეკითხებით, გავაჩეროთ თუ არა პროცესი.
    //უარის შემთხვევაში განახლება წყდება.
    private bool StopProgramRunningFromBin(string projectFolderPath)
    {
        string processName = Path.GetFileNameWithoutExtension(_mainProjectFileName);
        string binFolderPath = Path.Combine(projectFolderPath, "bin") + Path.DirectorySeparatorChar;
        Process[] processes = Process.GetProcessesByName(processName);
        try
        {
            Process[] runningFromBin = [.. processes.Where(x => IsRunningFrom(x, binFolderPath))];
            if (runningFromBin.Length == 0)
            {
                return true;
            }

            string processIds = string.Join(", ", runningFromBin.Select(x => x.Id));
            _logger.LogWarning("Program {ProcessName} (PID {ProcessIds}) is running from {BinFolderPath}",
                processName, processIds, binFolderPath);

            if (!_confirm($"Stop {processName} (PID {processIds}) to continue?"))
            {
                _logger.LogError("Update cancelled, because {ProcessName} is still running", processName);
                return false;
            }

            foreach (Process process in runningFromBin)
            {
                process.Kill(true);
                if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError("Cannot stop {ProcessName} (PID {ProcessId})", processName, process.Id);
                    }

                    return false;
                }
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Program {ProcessName} stopped", processName);
            }

            return true;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }

    internal static bool IsRunningFrom(Process process, string folderPath)
    {
        try
        {
            string? fileName = process.MainModule?.FileName;
            return fileName is not null && fileName.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
