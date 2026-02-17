using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppProjectCreator.React;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.ToolCommands;

public sealed class ReCreateReactAppFilesByTemplateNameToolCommand : ToolCommand
{
    private readonly ILogger _logger;
    private readonly string _reactAppName;
    private readonly string? _reactTemplateName;

    public ReCreateReactAppFilesByTemplateNameToolCommand(ILogger logger, string reactAppName,
        string? reactTemplateName, IParameters par, IParametersManager? parametersManager) : base(logger,
        "Recreate React app Files", par, parametersManager, "Recreate React app Files")
    {
        _logger = logger;
        _reactAppName = reactAppName;
        _reactTemplateName = reactTemplateName;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (ParametersManager is null)
        {
            _logger.LogError("ParametersManager is null");
            return ValueTask.FromResult(false);
        }

        var supportToolsParameters = (SupportToolsParameters?)ParametersManager.Parameters;

        if (supportToolsParameters is null)
        {
            _logger.LogError("SupportToolsParameters is null");
            return ValueTask.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            _logger.LogError("supportToolsParameters.WorkFolder is empty");
            return ValueTask.FromResult(false);
        }

        var reCreateReactAppFiles = new ReCreateReactAppFiles(_logger, supportToolsParameters.WorkFolder,
#pragma warning disable CA1308
            _reactAppName.ToLowerInvariant(), _reactTemplateName);
#pragma warning restore CA1308
        return ValueTask.FromResult(reCreateReactAppFiles.Run());
    }
}
