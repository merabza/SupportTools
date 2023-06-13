using CliParameters;
using LibAppProjectCreator.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.ToolCommands;

public sealed class JetBrainsCleanupCodeRunner : ToolCommand
{
    private readonly JetBrainsCleanupCodeRunnerParameters _parameters;

    public JetBrainsCleanupCodeRunner(ILogger logger, bool useConsole, JetBrainsCleanupCodeRunnerParameters parameters)
        : base(logger, useConsole, "jb CleanupCode", parameters, null, "Jet Brains Cleanup Code Runner")
    {
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //დეველოპერ ბაზაში მონაცემების ჩაყრის პროცესის გაშვება არსებული პროექტის საშუალებით და არსებული json ფაილების გამოყენებით
        return StShared.RunProcess(true, Logger, "jb", $"cleanupcode {_parameters.SolutionFileName}");
    }
}