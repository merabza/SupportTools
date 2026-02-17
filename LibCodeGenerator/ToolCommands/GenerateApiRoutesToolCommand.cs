using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibCodeGenerator.CodeCreators;
using LibCodeGenerator.Helpers;
using LibCodeGenerator.Models;
using LibGitData;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibCodeGenerator.ToolCommands;

public sealed class GenerateApiRoutesToolCommand : ToolCommand
{
    private const string Routes = nameof(Routes);
    private const string ApiRoutes = nameof(ApiRoutes);
    private const string CsFileExtension = ".cs";
    private readonly ILogger _logger;
    private readonly GenerateApiRoutesToolParameters _parameters;
    private readonly IParametersManager _parametersManager;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateApiRoutesToolCommand(ILogger logger, IParametersManager parametersManager,
        GenerateApiRoutesToolParameters parameters, bool useConsole = true) : base(logger, "ApiRoute Generator",
        parameters, null, "Generates ApiRoute class file")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _parameters = parameters;
        _useConsole = useConsole;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        string? workFolder = supportToolsParameters.CodeGenerateTestFolder;

        if (string.IsNullOrWhiteSpace(workFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.CodeGenerateTestFolder is not specified", true, Logger);
            return ValueTask.FromResult(false);
        }

        //შემოწმდეს სამუშაო ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        if (FileStat.CreateFolderIfNotExists(workFolder, true) == null)
        {
            StShared.WriteErrorLine($"does not exists and cannot create work folder {workFolder}", true, Logger);
            return ValueTask.FromResult(false);
        }

        string projectName = _parameters.ProjectName;
        ProjectModel project = _parameters.Project;
        string? apiContractsProjectName = project.ApiContractsProjectName;

        string projectFolder = Path.Combine(workFolder, projectName);

        //შემოწმდეს სამუშაო ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        if (FileStat.CreateFolderIfNotExists(projectFolder, true) == null)
        {
            StShared.WriteErrorLine($"does not exists and cannot create work folder {projectFolder}", true, Logger);
            return ValueTask.FromResult(false);
        }

        //შემოწმდესა და განახლდეს გიტის პროექტები
        var gitProjects = GitProjects.Create(Logger, supportToolsParameters.GitProjects);
        var gitRepos = GitRepos.Create(Logger, supportToolsParameters.Gits,
            project.SpaProjectFolderRelativePath(gitProjects), _useConsole, false);

        foreach (string gitProjectName in _parameters.Project.GetGitProjectNamesByGitCollectionType(EGitCol.Main))
        {
            string gitProjectFolder = Path.Combine(projectFolder, gitProjectName);

            GitData? gitData = gitRepos.GetGitRepoByKey(gitProjectName);
            if (gitData is null)
            {
                StShared.WriteErrorLine($"git with name {gitProjectName} is not exists", true, Logger);
                return ValueTask.FromResult(false);
            }

            Console.WriteLine($"---=== {gitProjectName} ===---");

            var gitOneProjectUpdater = new GitOneProjectUpdater(_logger, gitProjectFolder, gitData);
            GitProcessor? gitProcessor = gitOneProjectUpdater.UpdateOneGitProject();

            if (gitProcessor is null)
            {
                return ValueTask.FromResult(false);
            }
        }

        // Find the ApiRoutes class file
        string? apiContractsProjectFilePath =
            ApiContractsProjectFinder.FindApiContractsProject(projectFolder, apiContractsProjectName);
        if (apiContractsProjectFilePath is null)
        {
            _logger.LogError("Api Contracts Project file not found.");
            return ValueTask.FromResult(false);
        }

        var apiContractsProjectFile = new FileInfo(apiContractsProjectFilePath);

        string? apiContractsProjectFolder = apiContractsProjectFile.Directory?.FullName;
        if (apiContractsProjectFolder is null)
        {
            _logger.LogError("apiContractsProjectFolder not found.");
            return ValueTask.FromResult(false);
        }

        var versionsAndRoots = project.RouteClasses.Values.Select(x => new { x.Version, x.Root }).Distinct();

        foreach (var versionAndRoot in versionsAndRoots)
        {
            string version = versionAndRoot.Version;
            string root = versionAndRoot.Root;
            string versionFolderName = version.Capitalize();
            string versionApiRouteFolderPath = Path.Combine(apiContractsProjectFolder, versionFolderName, Routes);
            string versionApiRouteClassName = $"{projectName}{ApiRoutes}";
            string versionApiRouteClassFileName = $"{versionApiRouteClassName}{CsFileExtension}";
            string versionApiRouteClassFilePath = Path.Combine(versionApiRouteFolderPath, versionApiRouteClassFileName);

            //შემოწმდეს როუტების ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
            if (FileStat.CreateFolderIfNotExists(versionApiRouteFolderPath, true) == null)
            {
                StShared.WriteErrorLine($"does not exists and cannot create routes folder {versionApiRouteFolderPath}",
                    true, Logger);
                return ValueTask.FromResult(false);
            }

            if (File.Exists(versionApiRouteClassFilePath))
            {
                File.Delete(versionApiRouteClassFilePath);
            }

            string classNamespace = $"{apiContractsProjectName}.{versionFolderName}.{Routes}";

            var routesClassCreator = new RoutesClassCreator(_logger, versionApiRouteFolderPath, classNamespace,
                versionApiRouteClassFileName, versionApiRouteClassFileName, version, root);
            routesClassCreator.CreateFileStructure();
        }

        //// Generate ApiRoutes class code with all required routes
        //string generatedCode = ApiRoutesGenerator.GenerateApiRoutesClass(_parameters);

        //// Write generated code to the corresponding file
        //await File.WriteAllTextAsync(apiRoutesFilePath, generatedCode, cancellationToken);

        //// Show git diff for the changed code
        //string diff = await GitHelper.GetFileDiffAsync(apiRoutesFilePath);
        //Console.WriteLine("Changes in ApiRoutes class:");
        //Console.WriteLine(diff);

        //// Show success message
        //Console.WriteLine("ApiRoutes code successfully generated.");

        return ValueTask.FromResult(true);
    }
}
