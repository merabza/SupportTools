using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppProjectCreator.Models;
using Microsoft.Extensions.Logging;
using SystemTools.JetBrainsResharperGlobalToolsWork;

namespace LibAppProjectCreator.ToolCommands;

public sealed class JetBrainsCleanupCodeRunnerToolCommand : ToolCommand
{
    private readonly ILogger _logger;
    private readonly JetBrainsCleanupCodeRunnerParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public JetBrainsCleanupCodeRunnerToolCommand(ILogger logger, JetBrainsCleanupCodeRunnerParameters parameters) :
        base(logger, "jb CleanupCode", parameters, null, "Jet Brains Cleanup Code Runner")
    {
        _logger = logger;
        _parameters = parameters;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //დეველოპერ ბაზაში მონაცემების ჩაყრის პროცესის გაშვება არსებული პროექტის საშუალებით და არსებული json ფაილების გამოყენებით
        var processor = new JetBrainsResharperGlobalToolsProcessor(_logger, true);
        return ValueTask.FromResult(processor.Cleanupcode(_parameters.SolutionFileName, true).IsNone);
    }
}