using System;
using System.IO;
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

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator;

public sealed class AppProjectCreatorByTemplateToolAction : ToolAction
{
    private const string ActionName = "Create Application";
    public const string ActionDescription = "Create Application";
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _templateName;
    private readonly ETestOrReal _testOrReal;

    public AppProjectCreatorByTemplateToolAction(ILogger logger, IParametersManager parametersManager,
        string templateName, ETestOrReal testOrReal, bool useConsole) : base(logger, ActionName, null, null, useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _templateName = templateName;
        _testOrReal = testOrReal;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
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
        //string? tempFolderPath;
        string projectName;
        string? projectShortName;
        var appCreatorDataFolderFullName = Path.Combine(supportToolsParameters.WorkFolder, "AppCreatorData");
        switch (_testOrReal)
        {
            case ETestOrReal.Test:
                projectsFolderPath = Path.Combine(appCreatorDataFolderFullName, "Projects");
                secretsFolderPath = Path.Combine(appCreatorDataFolderFullName, "Security");
                //tempFolderPath = Path.Combine(appCreatorDataFolderFullName, "Temp");
                projectName = templateModel.TestProjectName;
                projectShortName = templateModel is { SupportProjectType: ESupportProjectType.Api, UseDatabase: true }
                    ? templateModel.TestProjectShortName
                    : null;
                break;
            case ETestOrReal.Real:
                projectsFolderPath = parameters.ProjectsFolderPathReal;
                secretsFolderPath = parameters.SecretsFolderPathReal;
                //tempFolderPath = Path.Combine(appCreatorDataFolderFullName, "TempReal");
                projectName = Inputer.InputTextRequired("New project name", "");
                projectShortName = templateModel is { SupportProjectType: ESupportProjectType.Api, UseDatabase: true }
                    ? Inputer.InputTextRequired("New project short name", "")
                    : null;
                break;
            default:
                throw new Exception("Unknown Test or Real");
        }

        projectName = projectName.ToNormalClassName();

        var par = AppProjectCreatorData.Create(_logger, projectName, projectShortName, templateModel.SupportProjectType,
            projectName, projectsFolderPath, secretsFolderPath, parameters.IndentSize);

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

        var appCreator = AppCreatorFabric.CreateAppCreator(_logger, par, templateModel,
            GitProjects.Create(_logger, supportToolsParameters.GitProjects),
            GitRepos.Create(_logger, supportToolsParameters.Gits, null, null, UseConsole),
            supportToolsParameters.WorkFolder, supportToolsParameters.ReactAppTemplates);

        if (appCreator is null)
        {
            _logger.LogError("appCreator does not created for project {projectName}", projectName);
            return false;
        }

        if (!await appCreator.PrepareParametersAndCreateApp(cancellationToken))
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

        var projectRecordCreator =
            new ProjectRecordCreator(_logger, _parametersManager, templateModel, projectName, projectShortName, "");

        if (projectRecordCreator.Create())
            return true;

        StShared.ConsoleWriteInformationLine(_logger, true,
            "code for project with name {0} created, but record create failed", projectName);
        return true;
    }
}