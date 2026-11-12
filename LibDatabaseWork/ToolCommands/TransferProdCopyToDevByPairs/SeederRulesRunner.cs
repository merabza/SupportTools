using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

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
        return RunnerProcess.RunAndParseRows("DataSeederRules", projectFilePath, tableName, DotnetExecutable,
            arguments, logger);
    }
}
