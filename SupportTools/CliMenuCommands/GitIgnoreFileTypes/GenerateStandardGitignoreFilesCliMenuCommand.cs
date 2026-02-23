using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Generators;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands.GitIgnoreFileTypes;

public sealed class GenerateStandardGitignoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateStandardGitignoreFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        "Generate standard .gitignore files...", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!Inputer.InputBool("This process will change .gitignore records, are you sure?", false, false))
        {
            return false;
        }

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var standardGitignoreFilesGenerator = new StandardGitignoreFilesGenerator(_logger, parameters);
        if (!standardGitignoreFilesGenerator.Generate() && Inputer.InputBool("Continue ans Save Changes?", false))
        {
            return false;
        }

        //შენახვა
        await _parametersManager.Save(parameters, "RunTimes generated success", null, cancellationToken);
        return true;
    }
}
