using System;
using System.Runtime.CompilerServices;
using AppCliTools.CliParameters;
using AppCliTools.CliTools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using Serilog;
using SupportTools;
using SupportTools.DependencyInjection;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Loading...");

    const string appName = "SupportTools";

    var argumentsAnalyzer = new ArgumentsAnalyzer();

    if (!await argumentsAnalyzer.Analysis(args))
    {
        return argumentsAnalyzer.ExitCode;
    }

    var parametersService = new ParametersService<SupportToolsParameters>(appName);

    switch (parametersService.Analysis(argumentsAnalyzer.ParametersFileName))
    {
        case EParseResult.Ok:
            break;
        case EParseResult.ShowHelp:
            argumentsAnalyzer.ShowHelp();
            return 1;
        case EParseResult.ParseError:
            StShared.WriteErrorLine($"File {argumentsAnalyzer.ParametersFileName} is not valid", true, logger, false);
            return 2;
        default:
            throw new SwitchExpressionException();
    }

    var serviceCollection = new ServiceCollection();

    // ReSharper disable once using
    await using ServiceProvider serviceProvider = serviceCollection
        .AddServices(appName, parametersService.Par!, parametersService.ParametersFileName!).BuildServiceProvider();

    //თუ --run მითითებულია, მენიუ არ გამოჩნდება, გაეშვება მითითებული ინსტრუმენტი და პროგრამა დაიხურება
    if (argumentsAnalyzer.ProjectTool is not null || argumentsAnalyzer.ServerTool is not null)
    {
        logger = serviceProvider.GetService<ILogger<Program>>();

        var parametersManager = serviceProvider.GetService<IParametersManager>();
        if (parametersManager is null)
        {
            StShared.WriteErrorLine("parametersManager is null", true, logger, false);
            return 6;
        }

        bool runSuccess = argumentsAnalyzer.ServerTool is null
            ? await ProjectToolRunner.Run(serviceProvider, parametersManager, argumentsAnalyzer.ProjectName!,
                argumentsAnalyzer.ProjectTool!.Value)
            : await ProjectToolRunner.RunOnServer(serviceProvider, parametersManager, argumentsAnalyzer.ProjectName!,
                argumentsAnalyzer.ServerName!, argumentsAnalyzer.ServerTool.Value);

        return runSuccess ? 0 : 7;
    }

    (CliAppLoopParameters? cliLoopPar, logger) = CliAppLoopParameters.Create<Program>(serviceProvider);
    if (cliLoopPar is null)
    {
        return 3;
    }

    var cliAppLoop = new CliAppLoop(cliLoopPar);

    return await cliAppLoop.Run() ? 0 : 100;
}
catch (Exception e)
{
    StShared.WriteException(e, true, logger);
    return 4;
}
finally
{
    await Log.CloseAndFlushAsync();
}
