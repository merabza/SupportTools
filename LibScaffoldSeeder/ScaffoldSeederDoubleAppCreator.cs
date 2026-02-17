using System.IO;
using System.Linq;
using System.Net.Http;
using LibAppProjectCreator.AppCreators;
using LibAppProjectCreator.Models;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SystemTools.SystemToolsShared;
using ToolsManagement.CompressionManagement;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibScaffoldSeeder;

public sealed class ScaffoldSeederDoubleAppCreator : DoubleAppCreator
{
    private const int IndentSize = 4;
    private const string ScaffoldSeederProjects = nameof(ScaffoldSeederProjects);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly string _projectTempFolderPath;
    private readonly string _projectWorkFolderPath;
    private readonly string _scaffoldSeederFolderName;
    private readonly string _scaffoldSeederFolderPath;
    private readonly ScaffoldSeederCreatorParameters _ssParameters;
    private readonly bool _useConsole;

    public ScaffoldSeederDoubleAppCreator(ILogger logger, IHttpClientFactory httpClientFactory, bool useConsole,
        ScaffoldSeederCreatorParameters ssParameters) : base(logger, useConsole)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _useConsole = useConsole;
        _ssParameters = ssParameters;
        _scaffoldSeederFolderName = $"{_ssParameters.ScaffoldSeederProjectName}ScaffoldSeeder";
        _projectWorkFolderPath = Path.Combine(_ssParameters.ScaffoldSeedersWorkFolder,
            _ssParameters.ScaffoldSeederProjectName);
        string scaffoldSeederSecurityFolderName = $"{_scaffoldSeederFolderName}.sec";
        SolutionSecurityFolderPath = Path.Combine(_projectWorkFolderPath, scaffoldSeederSecurityFolderName);
        _projectTempFolderPath = Path.Combine(_ssParameters.TempFolder, ScaffoldSeederProjects,
            _ssParameters.ScaffoldSeederProjectName);

        _scaffoldSeederFolderPath = Path.Combine(_projectWorkFolderPath, _scaffoldSeederFolderName);
        SolutionFolderPath = Path.Combine(_scaffoldSeederFolderPath, _scaffoldSeederFolderName);
    }

    public string SolutionSecurityFolderPath { get; }

    public string SolutionFolderPath { get; }

    public ScaffoldSeederCreatorData? ScaffoldSeederMainCreatorData { get; private set; }

    private ScaffoldSeederSolutionCreator? CreateAppCreator(bool forMain)
    {
        //შეიქმნას პროექტის შემქმნელი კლასისათვის საჭირო პარამეტრების ობიექტი
        var appCreatorParameters = AppProjectCreatorData.Create(_logger, _scaffoldSeederFolderName, string.Empty,
            string.Empty, ESupportProjectType.ScaffoldSeeder, _scaffoldSeederFolderName,
            forMain ? _projectWorkFolderPath : _projectTempFolderPath, SolutionSecurityFolderPath, IndentSize);

        //შევამოწმოთ შეიქმნა თუ არა პარამეტრები და თუ არა, გამოვიტანოთ შეცდომის შესახებ ინფორმაცია
        if (appCreatorParameters is null)
        {
            _logger.LogError(
                "AppProjectCreatorData does not created for project {ScaffoldSeederProjectName} ScaffoldSeeder",
                _ssParameters.ScaffoldSeederProjectName);
            return null;
        }

        //შეიქმნას აპლიკაციის შემქმნელი კლასისათვის საჭირო პარამეტრების ობიექტი
        var appCreatorBaseData = AppCreatorBaseData.Create(_logger, appCreatorParameters.WorkFolderPath,
            _scaffoldSeederFolderName, appCreatorParameters.SolutionFolderName,
            appCreatorParameters.SecurityWorkFolderPath, _ssParameters.GitIgnoreModelFilePaths);

        if (appCreatorBaseData is null)
        {
            StShared.WriteErrorLine("Error when creating Scaffold Seeder Solution Parameters", true, _logger);
            return null;
        }

        //შეიქმნას სკაფოლდ-სიდერის შემქმნელი კლასისათვის საჭირო პარამეტრების ობიექტი
        var scaffoldSeederCreatorData =
            ScaffoldSeederCreatorData.Create(appCreatorBaseData, _scaffoldSeederFolderName, _ssParameters);

        if (forMain)
        {
            ScaffoldSeederMainCreatorData = scaffoldSeederCreatorData;
        }

        return new ScaffoldSeederSolutionCreator(_logger, _httpClientFactory, _ssParameters, _scaffoldSeederFolderName,
            IndentSize, scaffoldSeederCreatorData);
    }

    protected override AppCreatorBase? CreateMainAppCreator()
    {
        ///////////////////////////////////////////////////////////////////////////////////

        const string reserveFolderName = "Reserve";

        string reserveFolderFullName = Path.Combine(_projectWorkFolderPath, reserveFolderName);

        //შევამოწმოთ არსებობს თუ არა ამ პროექტისთვის განკუთვნილი ფოლდერი და თუ არ არსებობს შევქმნათ
        string? checkedProjectWorkFolderPath = FileStat.CreateFolderIfNotExists(_projectWorkFolderPath, true);
        if (checkedProjectWorkFolderPath is null)
        {
            StShared.WriteErrorLine($"does not exists and can not be created work folder {_projectWorkFolderPath}",
                true, _logger);
            return null;
        }

        //შევამოწმოთ არსებობს თუ არა სარეზერვო არქივებისთვის განკუთვნილი ფოლდერი და თუ არ არსებობს შევქმნათ
        string? checkedReserveFolderFullPath = FileStat.CreateFolderIfNotExists(reserveFolderFullName, true);
        if (checkedReserveFolderFullPath is null)
        {
            StShared.WriteErrorLine($"does not exists and can not be created work folder {reserveFolderFullName}", true,
                _logger);
            return null;
        }

        string[] excl = [".vs", ".git", "obj", "bin"];

        var compressor = new Compressor(_useConsole, _logger, _ssParameters.SmartSchemaForLocal, "_ScaffoldSeeder_",
            excl.Select(s => $"*{Path.DirectorySeparatorChar}{s}{Path.DirectorySeparatorChar}*").ToArray());

        //შევამოწმოთ არსებობს თუ არა მიმდინარე სკაფოლდ-სიდინგის პროექტის შესაბამისი ფოლდერი
        if (Directory.Exists(_scaffoldSeederFolderPath) &&
            !compressor.CompressFolder(_scaffoldSeederFolderPath, checkedReserveFolderFullPath))
        {
            //თუ ფოლდერი არსებობს შევეცადოთ მის დაარქივებას სარეზერვო ფოლდერში
            StShared.WriteErrorLine($"{_scaffoldSeederFolderPath} does not compressed", true, _logger);
            return null;
        }

        //შევამოწმოთ არსებობს თუ არა ამ პროექტის სექურითი ფოლდერი და თუ არსებობს შევეცადოთ მისი დაარქივება სარეზერვო ფოლდერში
        if (!Directory.Exists(SolutionSecurityFolderPath))
        {
            return CreateAppCreator(true);
        }

        if (!compressor.CompressFolder(SolutionSecurityFolderPath, checkedReserveFolderFullPath))
        {
            StShared.WriteErrorLine($"{SolutionSecurityFolderPath} is not compressed", true, _logger);
            return null;
        }

        Directory.Delete(SolutionSecurityFolderPath, true);
        ///////////////////////////////////////////////////////////////////////////////////

        return CreateAppCreator(true);
    }

    protected override AppCreatorBase? CreateTempAppCreator()
    {
        return CreateAppCreator(false);
    }
}
