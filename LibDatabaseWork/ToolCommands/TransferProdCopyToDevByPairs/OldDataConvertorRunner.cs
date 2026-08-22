using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//OldDataConvertor-ის გამოძახება: ძველი სტრუქტურის ProdCopy ბაზიდან მონაცემების წამოღება გარე კონვერტორი პროგრამით.
//კონტრაქტი (SeederRules-ის ანალოგი + მიერთების სტრიქონი):
//  dotnet run --project "<csproj>" -- --table-name <DevTableName> --connection-string "<ProdCopy CS>" --log-folder "d:\Logs"
//StandardOutput-ში ბრუნდება JSON მასივი ერთ ხაზზე, მწკრივების გასაღებები Dev სვეტების სახელებია
public static class OldDataConvertorRunner
{
    private const string LogFolder = @"d:\Logs";
    private const string DotnetExecutable = "dotnet";

    public static List<Dictionary<string, object?>>? Run(string projectFilePath, string tableName,
        string prodCopyConnectionString, ILogger logger)
    {
        if (!File.Exists(projectFilePath))
        {
            StShared.WriteErrorLine($"OldDataConvertor project file '{projectFilePath}' does not exist", true, logger);
            return null;
        }

        string arguments =
            $"run --project \"{projectFilePath}\" -- --table-name {tableName} --connection-string \"{prodCopyConnectionString}\" --log-folder \"{LogFolder}\"";
        OneOf<(string, int), ErrorOmd[]> processResult =
            StShared.RunProcessWithOutput(false, logger, DotnetExecutable, arguments);
        if (processResult.IsT1)
        {
            StShared.WriteErrorLine($"OldDataConvertor process failed for table '{tableName}'", true, logger);
            return null;
        }

        string stdout = processResult.AsT0.Item1;
        return RunnerOutputParser.ParseRowsFromOutput(stdout, tableName, "OldDataConvertor", logger);
    }
}
