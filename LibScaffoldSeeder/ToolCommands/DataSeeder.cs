using CliParameters;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class DataSeeder : ToolCommand
{
    private readonly DataSeederParameters _parameters;

    public DataSeeder(ILogger logger, bool useConsole, DataSeederParameters parameters) : base(logger, useConsole,
        "Data Seeder", parameters, null, "Seeds data from existing Json files")
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

        if (string.IsNullOrWhiteSpace(_parameters.SeedProjectParametersFilePath))
        {
            Logger.LogError("Seed Project Parameters File Path does not specified");
            return false;
        }

        return true;
    }

    protected override bool RunAction()
    {
        //დეველოპერ ბაზაში მონაცემების ჩაყრის პროცესის გაშვება არსებული პროექტის საშუალებით და არსებული json ფაილების გამოყენებით
        return StShared.RunProcess(true, Logger, "dotnet",
            $"run --project {_parameters.SeedProjectFilePath} --use {_parameters.SeedProjectParametersFilePath}");
    }
}