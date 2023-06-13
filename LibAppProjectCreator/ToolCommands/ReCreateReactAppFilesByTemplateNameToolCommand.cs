using CliParameters;
using LibAppProjectCreator.React;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace LibAppProjectCreator.ToolCommands;

public sealed class ReCreateReactAppFilesByTemplateNameToolCommand : ToolCommand
{
    private readonly string _reactAppName;
    private readonly string? _reactTemplateName;

    public ReCreateReactAppFilesByTemplateNameToolCommand(ILogger logger, bool useConsole, string reactAppName,
        string? reactTemplateName, IParameters par, IParametersManager? parametersManager) : base(logger, useConsole,
        "Recreate React app Files", par, parametersManager)
    {
        _reactAppName = reactAppName;
        _reactTemplateName = reactTemplateName;
    }

    protected override bool RunAction()
    {
        if (ParametersManager is null)
        {
            Logger.LogError("ParametersManager is null");
            return false;
        }

        var supportToolsParameters = (SupportToolsParameters?)ParametersManager.Parameters;


        if (supportToolsParameters is null)
        {
            Logger.LogError("SupportToolsParameters is null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            Logger.LogError("supportToolsParameters.WorkFolder is empty");
            return false;
        }

        var reCreateReactAppFiles = new ReCreateReactAppFiles(Logger,
            supportToolsParameters.WorkFolder, _reactAppName.ToLower(), _reactTemplateName);
        return reCreateReactAppFiles.Run();
    }
}