using CliMenu;
using LibDataInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Generators;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands.GitIgnoreFileTypes;

public sealed class SyncUpGitignoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncUpGitignoreFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        "Sync Up .gitignore files...", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        if (!Inputer.InputBool(
                "This process will upload .gitignore records to server. Not Match records on the server will be deleted, New records will be created. Existing records will be modified as needed. are you sure?",
                false, false))
            return false;

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var standardGitignoreFilesGenerator = new StandardGitignoreFilesGenerator(_logger, parameters);
        if (!standardGitignoreFilesGenerator.Generate() && Inputer.InputBool("Continue ans Save Changes?", false))
            return false;
        //შენახვა
        _parametersManager.Save(parameters, "RunTimes generated success");
        return true;
    }
}