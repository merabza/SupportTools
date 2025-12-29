using CliMenu;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Generators;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands.GitIgnoreFileTypes;

public sealed class CheckAndGenerateGuidKeysGitignoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckAndGenerateGuidKeysGitignoreFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) :
        base("Check And Generate Guid Keys for .gitignore file types...", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        if (!Inputer.InputBool(
                "This will Check And if any not have, will Generate Guid Keys for .gitignore file types. are you sure?",
                false, false))
            return false;

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //შენახვა
        _parametersManager.Save(parameters, "Check And Generate Guid Keys success");
        return true;
    }
}