using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//SeederRules-ისა და OldDataConvertor-ის პროცესის გაშვება და მისი გამოტანიდან მწკრივების ამოღება. გამოტანა თავად
//მონაცემებია (მაგ. მომხმარებლების ელფოსტები, სახელები და პაროლის hash-ები), OldDataConvertor-ის არგუმენტებში კი
//ProdCopy-ის მიერთების სტრიქონია, ამიტომ StShared პროცესს ლოგერისა და კონსოლის გარეშე უშვებს: ბრძანებას, stdout-ს,
//stderr-სა და მისი შეცდომის ტექსტს არსად წერს. ლოგში მხოლოდ წყარო, პროექტი და ცხრილი ხვდება
internal static class RunnerProcess
{
    public static List<Dictionary<string, object?>>? RunAndParseRows(string sourceLabel, string projectFilePath,
        string tableName, string programFileName, string arguments, ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Running {SourceLabel} {ProjectFilePath} for table {TableName}", sourceLabel,
                projectFilePath, tableName);
        }

        Result<(string, int)> processResult = StShared.RunProcessWithOutput(false, null, programFileName, arguments);
        if (processResult.IsFailure)
        {
            StShared.WriteErrorLine(
                $"{sourceLabel} process failed for table '{tableName}' (its output can contain data and is not shown: run the project by hand to see it)",
                true, logger);
            return null;
        }

        return RunnerOutputParser.ParseRowsFromOutput(processResult.Value.Item1, tableName, sourceLabel, logger);
    }
}
