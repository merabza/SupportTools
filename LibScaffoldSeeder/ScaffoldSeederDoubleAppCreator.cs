using System;
using System.IO;
using CompressionManagement;
using FileManagersMain;
using LibAppProjectCreator.AppCreators;
using LibAppProjectCreator.Models;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SystemToolsShared;
// ReSharper disable ConvertToPrimaryConstructor

namespace LibScaffoldSeeder;

public class ScaffoldSeederDoubleAppCreator : DoubleAppCreator
{
    private readonly ILogger _logger;
    private readonly bool _useConsole;
    private readonly ScaffoldSeederCreatorParameters _ssParameters;
    private readonly string _scaffoldSeederFolderName;
    private readonly string _projectWorkFolderPath;
    private readonly string _projectTempFolderPath;
    private readonly string _scaffoldSeederFolderPath;

    private const int IndentSize = 4;

    public string SolutionSecurityFolderPath { get; }
    //public ScaffoldSeederCreatorData? ScaffoldSeederTempCreatorData { get; private set; }
    public ScaffoldSeederCreatorData? ScaffoldSeederMainCreatorData { get; private set; }
    //public string SolutionFolderPath { get; }

    //public ScaffoldSeederCreatorData? ScaffoldSeederCreatorData => _scaffoldSeederCreatorData;

    public ScaffoldSeederDoubleAppCreator(ILogger logger, bool useConsole, ScaffoldSeederCreatorParameters ssParameters)
        : base(logger, useConsole)
    {
        _logger = logger;
        _useConsole = useConsole;
        _ssParameters = ssParameters;
        _scaffoldSeederFolderName = $"{_ssParameters.ScaffoldSeederProjectName}ScaffoldSeeder";
        _projectWorkFolderPath =
            Path.Combine(_ssParameters.ScaffoldSeedersWorkFolder, _ssParameters.ScaffoldSeederProjectName);
        var scaffoldSeederSecurityFolderName = $"{_scaffoldSeederFolderName}.sec";
        SolutionSecurityFolderPath = Path.Combine(_projectWorkFolderPath, scaffoldSeederSecurityFolderName);
        _projectTempFolderPath = Path.Combine(_ssParameters.TempFolder, _ssParameters.ScaffoldSeederProjectName);

        _scaffoldSeederFolderPath = Path.Combine(_projectWorkFolderPath, _scaffoldSeederFolderName);
        //SolutionFolderPath = Path.Combine(_scaffoldSeederFolderPath, _scaffoldSeederFolderName);

    }

    private ScaffoldSeederSolutionCreator? CreateAppCreator(bool forMain)
    {

        //შეიქმნას პროექტის შემქმნელი კლასისათვის საჭირო პარამეტრების ობიექტი
        var appCreatorParameters = AppProjectCreatorData.Create(_logger, _scaffoldSeederFolderName, "",
            ESupportProjectType.ScaffoldSeeder, _scaffoldSeederFolderName,
            forMain ? _projectWorkFolderPath : _projectTempFolderPath,
            SolutionSecurityFolderPath, _ssParameters.LogFolder, IndentSize);

        //შევამოწმოთ შეიქმნა თუ არა პარამეტრები და თუ არა, გამოვიტანოთ შეცდომის შესახებ ინფორმაცია
        if (appCreatorParameters is null)
        {
            _logger.LogError(
                "AppProjectCreatorData does not created for project {Parameters.ScaffoldSeederProjectName} ScaffoldSeeder",
                _ssParameters.ScaffoldSeederProjectName);
            return null;
        }

        //შეიქმნას აპლიკაციის შემქმნელი კლასისათვის საჭირო პარამეტრების ობიექტი
        var appCreatorBaseData = AppCreatorBaseData.Create(_logger, appCreatorParameters.WorkFolderPath,
            _scaffoldSeederFolderName, appCreatorParameters.SolutionFolderName,
            appCreatorParameters.SecurityWorkFolderPath);

        if (appCreatorBaseData is null)
        {
            StShared.WriteErrorLine("Error when creating Scaffold Seeder Solution Parameters", true, _logger);
            return null;
        }

        //შეიქმნას სკაფოლდ-სიდერის შემქმნელი კლასისათვის საჭირო პარამეტრების ობიექტი
        var scaffoldSeederCreatorData =
            ScaffoldSeederCreatorData.Create(appCreatorBaseData, _scaffoldSeederFolderName, _ssParameters);

        if (forMain)
            ScaffoldSeederMainCreatorData = scaffoldSeederCreatorData;
        //else
        //    ScaffoldSeederTempCreatorData = scaffoldSeederCreatorData;

        return new ScaffoldSeederSolutionCreator(_logger, _ssParameters, _scaffoldSeederFolderName, IndentSize,
            scaffoldSeederCreatorData);
    }

    protected override AppCreatorBase? CreateMainAppCreator()
    {
        ///////////////////////////////////////////////////////////////////////////////////

        const string reserveFolderName = "Reserve";

        var reserveFolderFullName = Path.Combine(_projectWorkFolderPath, reserveFolderName);

        //შევამოწმოთ არსებობს თუ არა ამ პროექტისთვის განკუთვნილი ფოლდერი და თუ არ არსებობს შევქმნათ
        var checkedProjectWorkFolderPath = FileStat.CreateFolderIfNotExists(_projectWorkFolderPath, true);
        if (checkedProjectWorkFolderPath is null)
        {
            StShared.WriteErrorLine($"does not exists and can not be created work folder {_projectWorkFolderPath}",
                true,
                _logger);
            return null;
        }

        //შევამოწმოთ არსებობს თუ არა სარეზერვო არქივებისთვის განკუთვნილი ფოლდერი და თუ არ არსებობს შევქმნათ
        var checkedReserveFolderFullPath = FileStat.CreateFolderIfNotExists(reserveFolderFullName, true);
        if (checkedReserveFolderFullPath is null)
        {
            StShared.WriteErrorLine($"does not exists and can not be created work folder {reserveFolderFullName}", true,
                _logger);
            return null;
        }

        //შევამოწმოთ არსებობს თუ არა მიმდინარე სკაფოლდ-სიდინგის პროექტის შესაბამისი ფოლდერი
        if (Directory.Exists(_scaffoldSeederFolderPath))
            //თუ ფოლდერი არსებობს შევეცადოთ მის დაარქივებას სარეზერვო ფოლდერში
            if (!CompressFolder(_scaffoldSeederFolderPath, checkedReserveFolderFullPath))
            {
                StShared.WriteErrorLine($"{_scaffoldSeederFolderPath} does not compressed", true, _logger);
                return null;
            }

        ////შევამოწმოთ არსებობს თუ არა სოლუშენის ფოლდერი
        ////და თუ არსებობს წავშალოთ 
        ////ToDo აქ გვაქვს გადასაკეთებელი ისე, რომ ეს ფოლდერი კი არ წაიშალოს,
        ////არამედ მოხდეს სოლუშენის დროებით ფოლდერში შექმნა და შემდეგ არსებულ ფაილებთან და ფოლდერებთან განსხვავების დადგენა და ამ განსხვავების გადმოტანა დროებითი ფოლდერიდან ამ ფოლდერში
        //if (Directory.Exists(SolutionFolderPath))
        //    Directory.Delete(SolutionFolderPath, true);

        //შევამოწმოთ არსებობს თუ არა ამ პროექტის სექურითი ფოლდერი და თუ არსებობს შევეცადოთ მისი დაარქივება სარეზერვო ფოლდერში
        if (Directory.Exists(SolutionSecurityFolderPath))
        {
            if (!CompressFolder(SolutionSecurityFolderPath, checkedReserveFolderFullPath))
            {
                StShared.WriteErrorLine($"{SolutionSecurityFolderPath} does not compressed", true, _logger);
                return null;
            }

            //ToDo აქ გვაქვს გადასაკეთებელი ისე, რომ ეს ფოლდერი კი არ წაიშალოს,
            //არამედ მოხდეს შესაბამისი ფაილებისა და ფოლდერების დროებით ფოლდერში შექმნა და შემდეგ არსებულ ფაილებთან და ფოლდერებთან განსხვავების დადგენა და ამ განსხვავების გადმოტანა დროებითი ფოლდერიდან ამ ფოლდერში
            Directory.Delete(SolutionSecurityFolderPath, true);
            //ForceDeleteDirectory(solutionSecurityFolderPath);ამან არ იმუშავა
        }
        ///////////////////////////////////////////////////////////////////////////////////

        return CreateAppCreator(true);

    }

    protected override AppCreatorBase? CreateTempAppCreator()
    {
        //შევამოწმოთ არსებობს თუ არა სოლუშენის ფოლდერი დროებითი ფოლდერების მხარეს
        //და თუ არსებობს წავშალოთ 
        //if (Directory.Exists(SolutionFolderPath))
        //    Directory.Delete(SolutionFolderPath, true);

        var appCreator = CreateAppCreator(false);

        if (appCreator is null)
            return null;

        if (Directory.Exists(appCreator.SolutionPath))
            Directory.Delete(appCreator.SolutionPath, true);

        return appCreator;
    }


    private bool CompressFolder(string sourceFolderFullPath, string localPath)
    {
        const string backupFileNameSuffix = ".zip";
        var archiver = ArchiverFabric.CreateArchiverByType(_useConsole, _logger, EArchiveType.ZipClass, null, null,
            backupFileNameSuffix);

        if (archiver is null)
        {
            StShared.WriteErrorLine("archiver does not created", true, _logger);
            return false;
        }

        const string dateMask = "yyyy_MM_dd_HHmmss";
        const string middlePart = "_ScaffoldSeeder_";
        const string tempExtension = ".go!";
        var dir = new DirectoryInfo(sourceFolderFullPath);

        var backupFileNamePrefix = $"{dir.Name}{middlePart}";

        var backupFileName = $"{backupFileNamePrefix}{DateTime.Now.ToString(dateMask)}{backupFileNameSuffix}";
        var backupFileFullName = Path.Combine(localPath, backupFileName);
        var tempFileName = $"{backupFileFullName}{tempExtension}";


        if (!archiver.SourcesToArchive(new[] { sourceFolderFullPath }, tempFileName, Array.Empty<string>()))
        {
            File.Delete(tempFileName);
            return false;
        }

        File.Move(tempFileName, backupFileFullName);

        var localFileManager = FileManagersFabric.CreateFileManager(_useConsole, _logger, localPath);
        //წაიშალოს ადრე შექმნილი დაძველებული ფაილები

        if (localFileManager is null)
        {
            StShared.WriteErrorLine("localFileManager does not created", true, _logger);
            return false;
        }

        localFileManager.RemoveRedundantFiles(backupFileNamePrefix, dateMask, backupFileNameSuffix,
            _ssParameters.SmartSchemaForLocal);

        return true;
    }

}