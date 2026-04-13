using System;
using System.Threading;
using AppCliTools.CliParameters;
using AppCliTools.CliTools;
using AppCliTools.CliTools.DependencyInjection;
using AppCliTools.CliTools.Services.MenuBuilder;
using AppCliTools.CliTools.Services.RecentCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using SupportTools;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Loading...");

    const string appName = "Support Tools";

    var argParser = new ArgumentsParser<SupportToolsParameters>(args, appName, null);

    if (argParser.Analysis() != EParseResult.Ok)
    {
        return 1;
    }

    var par = (SupportToolsParameters?)argParser.Par;
    if (par is null)
    {
        StShared.WriteErrorLine("SupportToolsParameters is null", true);
        return 3;
    }

    string? parametersFileName = argParser.ParametersFileName;
    if (string.IsNullOrWhiteSpace(parametersFileName))
    {
        StShared.WriteErrorLine("parametersFileName is null or empty", true);
        return 3;
    }

    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSerilogLoggerService(LogEventLevel.Information, appName, par.LogFolder).AddServices()
        .AddMenuCommandsFactoryStrategies().AddToolCommandsFactoryStrategies().AddApplication(x =>
        {
            x.AppName = appName;
        }).AddMainParametersManager(x =>
        {
            x.ParametersFileName = parametersFileName;
            x.Par = par;
        });

    if (!string.IsNullOrWhiteSpace(par.RecentCommandsFileName) && par.RecentCommandsCount > 0)
    {
        serviceCollection.AddRecentCommandsService(x =>
        {
            x.RecentCommandsFileName = par.RecentCommandsFileName;
            x.RecentCommandsCount = par.RecentCommandsCount;
        });
    }

    // ReSharper disable once using
    await using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

    logger = serviceProvider.GetService<ILogger<Program>>();
    if (logger is null)
    {
        StShared.WriteErrorLine("logger is null", true);
        return 5;
    }

    var app = serviceProvider.GetService<IApplication>();
    if (app is null)
    {
        StShared.WriteErrorLine("app is null", true);
        return 6;
    }

    var menuBuilder = serviceProvider.GetService<IMenuBuilder>();
    if (menuBuilder is null)
    {
        StShared.WriteErrorLine("menuBuilder is null", true);
        return 6;
    }

    var recentCommandsService = serviceProvider.GetService<IRecentCommandsService>();
    if (recentCommandsService is null)
    {
        StShared.WriteErrorLine("recentCommandsService is null", true);
        return 6;
    }

    var supportTools = new CliAppLoop(app, menuBuilder, recentCommandsService);

    // ReSharper disable once using
    // ReSharper disable once DisposableConstructor
    using var cts = new CancellationTokenSource();
    CancellationToken token = cts.Token;
    token.ThrowIfCancellationRequested();

    return await supportTools.Run(token) ? 0 : 100;
}
catch (Exception e)
{
    StShared.WriteException(e, true, logger);
    return 7;
}
