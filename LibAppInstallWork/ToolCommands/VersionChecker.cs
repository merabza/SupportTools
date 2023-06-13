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

    public VersionChecker(ILogger logger, bool useConsole, CheckVersionParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters, parametersManager,
        ActionDescription)
    {
    }

    private CheckVersionParameters CheckVersionParameters => (CheckVersionParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction = new(Logger, UseConsole,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, 1);
        if (!checkParametersVersionAction.Run())
            Logger.LogError($"project {CheckVersionParameters.ProjectName} parameters file check failed");
        //return false;


        //შევამოწმოთ გაშვებული პროგრამის ვერსია 
        CheckProgramVersionAction checkProgramVersionAction = new(Logger, UseConsole,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, 1);
        if (!checkProgramVersionAction.Run())
            Logger.LogError($"project {CheckVersionParameters.ProjectName} version check failed");
        //return false;

        return true;
    }
}