using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands.GitIgnoreFileTypes;

public sealed class CheckAndGenerateGuidKeysGitignoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckAndGenerateGuidKeysGitignoreFilesCliMenuCommand(IParametersManager parametersManager) : base(
        "Check And Generate Guid Keys for .gitignore file types...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!Inputer.InputBool(
                "This will Check And if any not have, will Generate Guid Keys for .gitignore file types. are you sure?",
                false, false))
        {
            return ValueTask.FromResult(false);
        }

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //შენახვა
        _parametersManager.Save(parameters, "Check And Generate Guid Keys success");
        return ValueTask.FromResult(true);
    }
}
