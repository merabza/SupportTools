using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppProjectCreator.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.ToolCommands;

public sealed class JetBrainsCleanupCodeRunner : ToolCommand
{
    private readonly ILogger _logger;
    private readonly JetBrainsCleanupCodeRunnerParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public JetBrainsCleanupCodeRunner(ILogger logger, JetBrainsCleanupCodeRunnerParameters parameters) : base(logger,
        "jb CleanupCode", parameters, null, "Jet Brains Cleanup Code Runner")
    {
        _logger = logger;
        _parameters = parameters;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //დეველოპერ ბაზაში მონაცემების ჩაყრის პროცესის გაშვება არსებული პროექტის საშუალებით და არსებული json ფაილების გამოყენებით
        return ValueTask.FromResult(StShared
            .RunProcess(true, _logger, "jb", $"cleanupcode {_parameters.SolutionFileName}").IsNone);
    }
}