using System;
using System.Net.Http;
using CliParameters;
using LibParameters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using SupportTools;
using SupportToolsData.Models;
using SystemToolsShared;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Loading...");

    const string appName = "SupportTools";

    //პროგრამის ატრიბუტების დაყენება 
    ProgramAttributes.Instance.AppName = appName;

    var argParser = new ArgumentsParser<SupportToolsParameters>(args, appName, null);

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
    var servicesCreator = new SupportToolsServicesCreator(par);
    // ReSharper disable once using
    var serviceProvider = servicesCreator.CreateServiceProvider(LogEventLevel.Information);
    if (serviceProvider == null)
    {
        Console.WriteLine("Logger not created");
        return 4;
    }

    logger = serviceProvider.GetService<ILogger<Program>>();
    if (logger is null)
    {
        StShared.WriteErrorLine("logger is null", true);
        return 5;
    }

    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
    if (httpClientFactory is null)
    {
        StShared.WriteErrorLine("httpClientFactory is null", true);
        return 6;
    }

    // ReSharper disable once using
    using var memoryCache = serviceProvider.GetService<IMemoryCache>();
    if (memoryCache is null)
    {
        StShared.WriteErrorLine("memoryCache is null", true);
        return 6;
    }

    var supportTools = new SupportToolsCliAppLoop(logger, httpClientFactory, memoryCache,
        new ParametersManager(parametersFileName, par));
    return supportTools.Run() ? 0 : 100;
}
catch (Exception e)
{
    StShared.WriteException(e, true, logger);
    return 7;
}