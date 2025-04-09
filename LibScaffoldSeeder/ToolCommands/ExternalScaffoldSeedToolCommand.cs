using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibDotnetWork;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class ExternalScaffoldSeedToolCommand : ToolCommand
{
    private readonly ILogger _logger;
    private readonly ExternalScaffoldSeedToolParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ExternalScaffoldSeedToolCommand(ILogger logger, ExternalScaffoldSeedToolParameters parameters) : base(logger,
        "Data Seeder", parameters, null, "Seeds data from existing Json files")
    {
        _logger = logger;
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //დეველოპერ ბაზაში მონაცემების ჩაყრის პროცესის გაშვება არსებული პროექტის საშუალებით და არსებული json ფაილების გამოყენებით
        var dotnetProcessor = new DotnetProcessor(_logger, true);
        return ValueTask.FromResult(dotnetProcessor
            .RunToolUsingParametersFile(_parameters.ProjectFilePath, _parameters.ProjectParametersFilePath).IsNone);
    }
}