using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator.Models;
using LibDataInput;
using LibGitData.Models;
using LibGitWork;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator;

public sealed class AppProjectCreatorByTemplateToolAction : ToolAction
{
    private const string ActionName = "Create Application";
    public const string ActionDescription = "Create Application";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _templateName;
    private readonly ETestOrReal _testOrReal;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppProjectCreatorByTemplateToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string templateName, ETestOrReal testOrReal, bool useConsole) : base(
        logger, ActionName, null, null, useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _templateName = templateName;
        _testOrReal = testOrReal;
        _httpClientFactory = httpClientFactory;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        var parameters = supportToolsParameters.AppProjectCreatorAllParameters;

        if (parameters is null)
        {
            _logger.LogError("AppProjectCreatorAllParameters is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            _logger.LogError("supportToolsParameters.WorkFolder is empty");
            return false;
        }

        var templateModel = parameters.GetTemplate(_templateName);
        if (templateModel is null)
        {
            _logger.LogError("templateModel is null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(templateModel.TestProjectName))
        {
            _logger.LogError("TestProjectName is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(templateModel.TestProjectShortName))
        {
            _logger.LogError("TestProjectShortName is empty");
            return false;
        }

        string? projectsFolderPath;
        string? secretsFolderPath;
        string projectName;
        string? projectShortName;
        string? dbPartProjectName;
        var appCreatorDataFolderFullName = Path.Combine(supportToolsParameters.WorkFolder, "AppCreatorData");
        switch (_testOrReal)
        {
            case ETestOrReal.Test:
                projectsFolderPath = Path.Combine(appCreatorDataFolderFullName, "Projects");
                secretsFolderPath = Path.Combine(appCreatorDataFolderFullName, "Security");
                projectName = templateModel.TestProjectName;
                projectShortName = templateModel is { SupportProjectType: ESupportProjectType.Api, UseDatabase: true }
                    ? templateModel.TestProjectShortName
                    : null;
                dbPartProjectName = $"{projectName}Db";
                break;
            case ETestOrReal.Real:
                projectsFolderPath = parameters.ProjectsFolderPathReal;
                secretsFolderPath = parameters.SecretsFolderPathReal;
                projectName = Inputer.InputTextRequired("New project name", string.Empty);
                projectShortName = templateModel is { SupportProjectType: ESupportProjectType.Api, UseDatabase: true }
                    ? Inputer.InputTextRequired("New project short name", string.Empty)
                    : null;
                dbPartProjectName = templateModel.UseDbPartFolderForDatabaseProjects
                    ? Inputer.InputTextRequired("New dbPart project name", string.Empty)
                    : null;
                break;
            default:
                throw new Exception("Unknown Test or Real");
        }

        projectName = projectName.ToNormalClassName();

        var par = AppProjectCreatorData.Create(_logger, projectName, projectShortName, dbPartProjectName,
            templateModel.SupportProjectType, projectName, projectsFolderPath, secretsFolderPath,
            parameters.IndentSize);

        if (par is null)
        {
            _logger.LogError("AppProjectCreatorData does not created for project {projectName}", projectName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            _logger.LogError("supportToolsParameters.WorkFolder is empty");
            return false;
        }

        var appCreator = AppCreatorFabric.CreateAppCreator(_logger, _httpClientFactory, par, templateModel,
            GitProjects.Create(_logger, supportToolsParameters.GitProjects),
            GitRepos.Create(_logger, supportToolsParameters.Gits, null, UseConsole, false),
            supportToolsParameters.GitIgnoreModelFilePaths);

        if (appCreator is null)
        {
            _logger.LogError("appCreator does not created for project {projectName}", projectName);
            return false;
        }

        if (!await appCreator.PrepareParametersAndCreateApp(_testOrReal == ETestOrReal.Real, cancellationToken))
            return false;

        if (_testOrReal != ETestOrReal.Real)
            return true;

        var existingProject = supportToolsParameters.GetProject(projectName);

        if (existingProject is not null)
        {
            StShared.ConsoleWriteInformationLine(_logger, true,
                "project with name {0} already exists and cannot be updated", projectName);
            return true;
        }

        if (!Inputer.InputBool($"Create record for project with name {projectName}?", true, false))
            return true;

        var projectRecordCreator = new ProjectRecordCreator(_logger, _parametersManager, templateModel, projectName,
            projectShortName, dbPartProjectName, string.Empty);

        if (projectRecordCreator.Create())
            return true;

        StShared.ConsoleWriteInformationLine(_logger, true,
            "code for project with name {0} created, but record create failed", projectName);
        return true;
    }
}