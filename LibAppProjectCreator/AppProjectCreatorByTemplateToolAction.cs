using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using LibAppProjectCreator.Models;
using LibDataInput;
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
    private readonly IParametersManager _parametersManager;
    private readonly string _templateName;
    private readonly ETestOrReal _testOrReal;

    public AppProjectCreatorByTemplateToolAction(ILogger logger, IParametersManager parametersManager,
        string templateName, ETestOrReal testOrReal) : base(logger, ActionName, null, null)
    {
        _parametersManager = parametersManager;
        _templateName = templateName;
        _testOrReal = testOrReal;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        var parameters = supportToolsParameters.AppProjectCreatorAllParameters;

        if (parameters is null)
        {
            Logger.LogError("AppProjectCreatorAllParameters is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            Logger.LogError("supportToolsParameters.WorkFolder is empty");
            return false;
        }

        var templateModel = parameters.GetTemplate(_templateName);
        if (templateModel is null)
        {
            Logger.LogError("templateModel is null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(templateModel.TestProjectName))
        {
            Logger.LogError("TestProjectName is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(templateModel.TestProjectShortName))
        {
            Logger.LogError("TestProjectShortName is empty");
            return false;
        }

        string? projectsFolderPath;
        string? secretsFolderPath;
        //string? tempFolderPath;
        string projectName;
        string projectShortName;
        var appCreatorDataFolderFullName = Path.Combine(supportToolsParameters.WorkFolder, "AppCreatorData");
        switch (_testOrReal)
        {
            case ETestOrReal.Test:
                projectsFolderPath = Path.Combine(appCreatorDataFolderFullName, "Projects");
                secretsFolderPath = Path.Combine(appCreatorDataFolderFullName, "Security");
                //tempFolderPath = Path.Combine(appCreatorDataFolderFullName, "Temp");
                projectName = templateModel.TestProjectName;
                projectShortName = templateModel.TestProjectShortName;
                break;
            case ETestOrReal.Real:
                projectsFolderPath = parameters.ProjectsFolderPathReal;
                secretsFolderPath = parameters.SecretsFolderPathReal;
                //tempFolderPath = Path.Combine(appCreatorDataFolderFullName, "TempReal");
                projectName = Inputer.InputTextRequired("New project name", "");
                projectShortName = Inputer.InputTextRequired("New project short name", "");
                break;
            default:
                throw new Exception("Unknown Test or Real");
        }

        projectName = projectName.ToNormalClassName();

        var par = AppProjectCreatorData.Create(Logger, projectName, projectShortName, templateModel.SupportProjectType,
            projectName, projectsFolderPath, secretsFolderPath, supportToolsParameters.LogFolder,
            parameters.IndentSize);

        if (par is null)
        {
            Logger.LogError("AppProjectCreatorData does not created for project {projectName}", projectName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            Logger.LogError("supportToolsParameters.WorkFolder is empty");
            return false;
        }

        var appCreator = AppCreatorFabric.CreateAppCreator(Logger, par, templateModel,
            GitProjects.Create(Logger, supportToolsParameters.GitProjects),
            GitRepos.Create(Logger, supportToolsParameters.Gits, null, null), supportToolsParameters.WorkFolder,
            supportToolsParameters.ReactAppTemplates);

        if (appCreator is null)
        {
            Logger.LogError("appCreator does not created for project {projectName}", projectName);
            return false;
        }

        if (!await appCreator.PrepareParametersAndCreateApp(cancellationToken))
            return false;

        if (_testOrReal != ETestOrReal.Real)
            return true;

        var existingProject = supportToolsParameters.GetProject(projectName);

        if (existingProject is not null)
        {
            StShared.ConsoleWriteInformationLine(Logger, true,
                "project with name {0} already exists and cannot be updated", projectName);
            return true;
        }

        if (!Inputer.InputBool($"Create record for project with name {projectName}?", true, false))
            return true;

        var projectRecordCreator =
            new ProjectRecordCreator(Logger, _parametersManager, projectName, projectShortName, "");

        if (projectRecordCreator.Create())
            return true;

        StShared.ConsoleWriteInformationLine(Logger, true,
            "code for project with name {0} created, but record create failed", projectName);
        return true;
    }
}