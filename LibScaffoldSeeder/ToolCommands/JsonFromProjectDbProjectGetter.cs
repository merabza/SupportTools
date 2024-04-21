using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibParameters;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class JsonFromProjectDbProjectGetter : ToolCommand
{
    private readonly ILogger _logger;
    private const string ActionName = "Get Json From Project DbProject";
    private const string ActionDescription = "Get Json From Project DbProject";

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    // ReSharper disable once ConvertToPrimaryConstructor
    public JsonFromProjectDbProjectGetter(ILogger logger,
        JsonFromProjectDbProjectGetterParameters jsonFromProjectDbProjectGetterParameters,
        IParametersManager parametersManager) : base(logger, ActionName, jsonFromProjectDbProjectGetterParameters,
        parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private JsonFromProjectDbProjectGetterParameters CorrectNewDbParameters =>
        (JsonFromProjectDbProjectGetterParameters)Par;

    protected override bool CheckValidate()
    {
        if (string.IsNullOrWhiteSpace(CorrectNewDbParameters.GetJsonFromScaffoldDbProjectFileFullName))
        {
            _logger.LogError("GetJsonFromScaffoldDbProjectFileFullName not specified");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(CorrectNewDbParameters.GetJsonFromScaffoldDbProjectParametersFileFullName))
            return true;

        _logger.LogError("GetJsonFromScaffoldDbProjectParametersFileFullName not specified");
        return false;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        return Task.FromResult(StShared.RunProcess(true, _logger, "dotnet",
                $"run --project {CorrectNewDbParameters.GetJsonFromScaffoldDbProjectFileFullName} --use {CorrectNewDbParameters.GetJsonFromScaffoldDbProjectParametersFileFullName}")
            .IsNone);
    }
}