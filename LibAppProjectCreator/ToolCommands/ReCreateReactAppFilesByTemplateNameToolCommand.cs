using CliParameters;
using LibAppProjectCreator.React;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using System.Threading.Tasks;
using System.Threading;
// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.ToolCommands;

public sealed class ReCreateReactAppFilesByTemplateNameToolCommand : ToolCommand
{
    private readonly string _reactAppName;
    private readonly string? _reactTemplateName;

    public ReCreateReactAppFilesByTemplateNameToolCommand(ILogger logger, string reactAppName,
        string? reactTemplateName, IParameters par, IParametersManager? parametersManager) : base(logger,
        "Recreate React app Files", par, parametersManager)
    {
        _reactAppName = reactAppName;
        _reactTemplateName = reactTemplateName;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        if (ParametersManager is null)
        {
            Logger.LogError("ParametersManager is null");
            return Task.FromResult(false);
        }

        var supportToolsParameters = (SupportToolsParameters?)ParametersManager.Parameters;


        if (supportToolsParameters is null)
        {
            Logger.LogError("SupportToolsParameters is null");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            Logger.LogError("supportToolsParameters.WorkFolder is empty");
            return Task.FromResult(false);
        }

        var reCreateReactAppFiles = new ReCreateReactAppFiles(Logger,
            supportToolsParameters.WorkFolder, _reactAppName.ToLower(), _reactTemplateName);
        return Task.FromResult(reCreateReactAppFiles.Run());
    }
}