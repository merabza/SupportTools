using CliParameters;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class DataSeeder : ToolCommand
{
    private readonly DataSeederParameters _parameters;

    public DataSeeder(ILogger logger, DataSeederParameters parameters) : base(logger, "Data Seeder", parameters, null,
        "Seeds data from existing Json files")
    {
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        if (string.IsNullOrWhiteSpace(_parameters.SeedProjectFilePath))
        {
            Logger.LogError("Seed Project File Path does not specified");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_parameters.SeedProjectParametersFilePath))
            return true;

        Logger.LogError("Seed Project Parameters File Path does not specified");
        return false;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        //დეველოპერ ბაზაში მონაცემების ჩაყრის პროცესის გაშვება არსებული პროექტის საშუალებით და არსებული json ფაილების გამოყენებით
        return Task.FromResult(StShared.RunProcess(true, Logger, "dotnet",
                $"run --project {_parameters.SeedProjectFilePath} --use {_parameters.SeedProjectParametersFilePath}")
            .IsNone);
    }
}