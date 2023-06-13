using CliParameters;
using LibParameters;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class JsonFromProjectDbProjectGetter : ToolCommand
{
    private const string ActionName = "Get Json From Project DbProject";
    private const string ActionDescription = "Get Json From Project DbProject";

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    public JsonFromProjectDbProjectGetter(ILogger logger, bool useConsole,
        JsonFromProjectDbProjectGetterParameters jsonFromProjectDbProjectGetterParameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName,
        jsonFromProjectDbProjectGetterParameters, parametersManager, ActionDescription)
    {
    }

    private JsonFromProjectDbProjectGetterParameters CorrectNewDbParameters =>
        (JsonFromProjectDbProjectGetterParameters)Par;

    protected override bool CheckValidate()
    {
        if (string.IsNullOrWhiteSpace(CorrectNewDbParameters.GetJsonFromScaffoldDbProjectFileFullName))
        {
            Logger.LogError("GetJsonFromScaffoldDbProjectFileFullName not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(CorrectNewDbParameters.GetJsonFromScaffoldDbProjectParametersFileFullName))
        {
            Logger.LogError("GetJsonFromScaffoldDbProjectParametersFileFullName not specified");
            return false;
        }

        return true;
    }

    protected override bool RunAction()
    {
        return StShared.RunProcess(true, Logger, "dotnet",
            $"run --project {CorrectNewDbParameters.GetJsonFromScaffoldDbProjectFileFullName} --use {CorrectNewDbParameters.GetJsonFromScaffoldDbProjectParametersFileFullName}");
    }
}