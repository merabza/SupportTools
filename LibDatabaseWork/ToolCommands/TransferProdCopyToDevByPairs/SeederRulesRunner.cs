using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//SeederRules-ის გამოძახება: dotnet პროექტი იშვება ცხრილის სახელით და დააბრუნებს StandardOutput-ში JSON-ის მასივს
public static class SeederRulesRunner
{
    private const string LogFolder = @"d:\Logs";
    private const string DotnetExecutable = "dotnet";

    public static List<Dictionary<string, object?>>? Run(string projectFilePath, string tableName, ILogger logger)
    {
        if (!File.Exists(projectFilePath))
        {
            StShared.WriteErrorLine($"DataSeederRules project file '{projectFilePath}' does not exist", true, logger);
            return null;
        }

        string arguments =
            $"run --project \"{projectFilePath}\" -- --table-name {tableName} --log-folder \"{LogFolder}\"";
        OneOf<(string, int), ErrorOmd[]> processResult =
            StShared.RunProcessWithOutput(false, logger, DotnetExecutable, arguments);
        if (processResult.IsT1)
        {
            StShared.WriteErrorLine($"DataSeederRules process failed for table '{tableName}'", true, logger);
            return null;
        }

        string stdout = processResult.AsT0.Item1;
        return RunnerOutputParser.ParseRowsFromOutput(stdout, tableName, "DataSeederRules", logger);
    }
}
