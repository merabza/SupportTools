using System;
using CliParameters;
using LibParameters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using SupportToolsData.Models;
using SystemToolsShared;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Loading...");

    //პროგრამის ატრიბუტების დაყენება 
    ProgramAttributes.Instance.SetAttribute("AppName", "Support Tools");

    var argParser = new ArgumentsParser<SupportToolsParameters>(args, "SupportTools", null);
    switch (argParser.Analysis())
    {
        case EParseResult.Ok: break;
        case EParseResult.Usage: return 1;
        case EParseResult.Error: return 2;
        default:
            throw new ArgumentOutOfRangeException();
    }

    var par = (SupportToolsParameters?)argParser.Par;
    if (par is null)
    {
        StShared.WriteErrorLine("SupportToolsParameters is null", true);
        return 3;
    }

    var parametersFileName = argParser.ParametersFileName;
    ServicesCreator servicesCreator = new(par.LogFolder, null, "SupportTools");
    using (var serviceProvider = servicesCreator.CreateServiceProvider(LogEventLevel.Information))
    {
        if (serviceProvider == null)
        {
            Console.WriteLine("Logger not created");
            return 8;
        }

        logger = serviceProvider.GetService<ILogger<Program>>();
        if (logger is null)
        {
            StShared.WriteErrorLine("logger is null", true);
            return 3;
        }
    }

    SupportTools.SupportTools supportTools = new(logger, new ParametersManager(parametersFileName, par));
    return supportTools.Run() ? 0 : 1;
}
catch (Exception e)
{
    StShared.WriteException(e, true, logger);
    return 7;
}