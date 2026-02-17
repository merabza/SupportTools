using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator.AppCreators;
using LibNpmWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.CompressionManagement;
using ToolsManagement.LibToolActions;

namespace LibAppProjectCreator.ToolActions;

public sealed class ReCreateUpdateFrontSpaProjectToolAction : ToolAction
{
    private const string ActionName = "ReCreate Update Front Spa Project";
    private const string FrontSpaProjects = nameof(FrontSpaProjects);
    private const string ProjectReserves = nameof(ProjectReserves);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly bool _useConsole;
    private string? _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReCreateUpdateFrontSpaProjectToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, bool useConsole) : base(logger, ActionName, null, null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _useConsole = useConsole;
    }

    public void SetProjectName(string projectName)
    {
        _projectName = projectName;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //შეიქმნას ცარელა spa front პროექტი დროებით ფოლდერში

        //დავადგინოთ დროებითი ფოლდერის სახელი, სადაც უნდა შეიქმნას ფრონტის პროექტი
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        if (string.IsNullOrWhiteSpace(supportToolsParameters.SmartSchemaNameForLocal))
        {
            StShared.WriteErrorLine("SmartSchemaNameForLocal does not specified", true);
            return ValueTask.FromResult(false);
        }

        SmartSchema smartSchemaForLocal =
            supportToolsParameters.GetSmartSchemaRequired(supportToolsParameters.SmartSchemaNameForLocal);

        if (string.IsNullOrWhiteSpace(supportToolsParameters.TempFolder))
        {
            StShared.WriteErrorLine("TempFolder does not specified in parameters", true);
            return ValueTask.FromResult(false);
        }

        if (string.IsNullOrEmpty(_projectName))
        {
            Console.WriteLine("Project name is not set.");
            return ValueTask.FromResult(false);
        }

        ProjectModel project = supportToolsParameters.GetProjectRequired(_projectName);

        if (string.IsNullOrWhiteSpace(project.SpaProjectName))
        {
            StShared.WriteErrorLine("SpaProjectName does not specified in parameters", true);
            return ValueTask.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(project.ProjectFolderName))
        {
            StShared.WriteErrorLine($"ProjectFolderName is not specified for project {_projectName}", true);
            return ValueTask.FromResult(false);
        }

        string createInPath = Path.Combine(supportToolsParameters.TempFolder, FrontSpaProjects, _projectName,
            $"{_projectName}Front");

        //რეაქტის პროექტის შექმნა ფრონტისთვის
        var reactEsProjectCreator = new ReactEsProjectCreator(_logger, _httpClientFactory, createInPath,
            project.SpaProjectName, $"{project.SpaProjectName}.esproj", project.SpaProjectName, true);

        if (Directory.Exists(createInPath))
        {
            FileStat.DeleteDirectoryWithNormaliseAttributes(createInPath);
        }

        if (!reactEsProjectCreator.Create())
        {
            return ValueTask.FromResult(false);
        }

        var npmProcessor = new NpmProcessor(_logger);
        string spaProjectPath = Path.Combine(createInPath, project.SpaProjectName);

        if (!npmProcessor.InstallNpmPackages(spaProjectPath))
        {
            return ValueTask.FromResult(false);
        }

        //დაინსტალირდეს პროექტის შესაბამისი npm პაკეტები
        if (project.FrontNpmPackageNames.Any(npmPackageName =>
                !npmProcessor.InstallNpmPackage(spaProjectPath, npmPackageName)))
        {
            return ValueTask.FromResult(false);
        }

        string reservePath = Path.Combine(supportToolsParameters.TempFolder, ProjectReserves, _projectName);

        //შევამოწმოთ არსებობს თუ არა სარეზერვო არქივებისთვის განკუთვნილი ფოლდერი და თუ არ არსებობს შევქმნათ
        string? checkedReserveFolderFullPath = FileStat.CreateFolderIfNotExists(reservePath, true);
        if (checkedReserveFolderFullPath is null)
        {
            StShared.WriteErrorLine($"does not exists and can not be created work folder {reservePath}", true, _logger);
            return ValueTask.FromResult(false);
        }

        const string nodeModulesFolderName = "node_modules";
        const string packageJsonFileName = "package.json";
        //აქედან დაწყეებული კოდი შეიცავს ინფრომაციის დაკარგვის საშიშროებას,
        //ამიტომ წინასწარ უნდა მოხდეს არსებული პროექტის გადანახვა
        //ამისათის გამოვიყენოთ დაარქივების იგივე მეთოდი, რომელიც გამოყენებულია სკაფოლსიდერის ინსტრუმენტში

        string[] excl = [".vs", ".git", "obj", "bin", nodeModulesFolderName];

        var compressor = new Compressor(_useConsole, _logger, smartSchemaForLocal, "_Reserve_",
            excl.Select(s => $"*{Path.DirectorySeparatorChar}{s}{Path.DirectorySeparatorChar}*").ToArray());

        compressor.CompressFolder(project.ProjectFolderName, checkedReserveFolderFullPath);

        //წაიშალოს არსებული პროექტის node_modules, obj ფოლდერები, .esproj, package.json, package-lock.json ფაილები
        string frontProjectFolder =
            Path.Combine(project.ProjectFolderName, $"{_projectName}Front", project.SpaProjectName);

        FileStat.DeleteDirectoryIfExists(Path.Combine(frontProjectFolder, nodeModulesFolderName));
        FileStat.DeleteDirectoryIfExists(Path.Combine(frontProjectFolder, "obj"));
        FileStat.DeleteDirectoryIfExists(Path.Combine(frontProjectFolder, "build"));

        FileStat.DeleteFileIfExists(Path.Combine(frontProjectFolder, $"{project.SpaProjectName}.esproj"));

        string frontPackageJsonFileName = Path.Combine(frontProjectFolder, packageJsonFileName);

        FileStat.DeleteFileIfExists(frontPackageJsonFileName);
        FileStat.DeleteFileIfExists(Path.Combine(frontProjectFolder, "package-lock.json"));

        //დაკოპირდეს დროებით ფოლდერში შექმნილი პროექტის package.json მიმდინარე პროექტრის შასაბამის ფოლდერში
        File.Copy(Path.Combine(spaProjectPath, packageJsonFileName), frontPackageJsonFileName);

        //გაეშვას npm install ბრძანება მიმდინარე პროექტისათვის

        return ValueTask.FromResult(npmProcessor.InstallNpmPackages(frontProjectFolder));

        //შემოწმდეს დროებით ფოლდერშ არსებული დანარჩენი ფაილები ემთხვევა თუ არა მიმდინარე პროექტის ფოლდერში არსებულ შესაბამის ფაილებს

        //აცდენის შესახებ ინფორმაცია გამოვიდეს კონსოლში
    }
}
