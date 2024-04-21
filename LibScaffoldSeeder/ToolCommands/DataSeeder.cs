using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class DataSeeder : ToolCommand
{
    private readonly ILogger _logger;
    private readonly DataSeederParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DataSeeder(ILogger logger, DataSeederParameters parameters) : base(logger, "Data Seeder", parameters, null,
        "Seeds data from existing Json files")
    {
        _logger = logger;
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        if (string.IsNullOrWhiteSpace(_parameters.SeedProjectFilePath))
        {
            _logger.LogError("Seed Project File Path does not specified");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_parameters.SeedProjectParametersFilePath))
            return true;

        _logger.LogError("Seed Project Parameters File Path does not specified");
        return false;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        //დეველოპერ ბაზაში მონაცემების ჩაყრის პროცესის გაშვება არსებული პროექტის საშუალებით და არსებული json ფაილების გამოყენებით
        return Task.FromResult(StShared.RunProcess(true, _logger, "dotnet",
                $"run --project {_parameters.SeedProjectFilePath} --use {_parameters.SeedProjectParametersFilePath}")
            .IsNone);
    }
}