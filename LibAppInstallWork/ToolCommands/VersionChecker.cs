//Created by ProjectMainClassCreator at 5/11/2021 08:52:10

using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class VersionChecker : ToolCommand
{
    private const string ActionName = "Check Version";
    private const string ActionDescription = "Check Version";

    public VersionChecker(ILogger logger, CheckVersionParameters parameters, IParametersManager parametersManager) :
        base(logger, ActionName, parameters, parametersManager, ActionDescription)
    {
    }

    private CheckVersionParameters CheckVersionParameters => (CheckVersionParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var projectName = CheckVersionParameters.ProjectName;
        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction = new(Logger, CheckVersionParameters.WebAgentForCheck,
            CheckVersionParameters.ProxySettings, null, 1);
        if (!checkParametersVersionAction.Run())
            Logger.LogError("project {projectName} parameters file check failed", projectName);
        //return false;


        //შევამოწმოთ გაშვებული პროგრამის ვერსია 
        CheckProgramVersionAction checkProgramVersionAction = new(Logger, CheckVersionParameters.WebAgentForCheck,
            CheckVersionParameters.ProxySettings, null, 1);
        if (!checkProgramVersionAction.Run())
            Logger.LogError("project {projectName} version check failed", projectName);
        //return false;

        return true;
    }
}