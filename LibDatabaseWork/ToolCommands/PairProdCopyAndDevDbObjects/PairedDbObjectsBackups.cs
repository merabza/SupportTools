using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

//წყვილების ფაილის ბექაპები, რომლებსაც ParametersManager.Save ფაილის ყოველი ცვლილებისას ქმნის:
//<ფაილი>.yyyyMMdd-HHmmss-fff.bak იმავე ფოლდერში (ინახება 10 უახლესი)
public static class PairedDbObjectsBackups
{
    private const string BackupDateMask = "yyyyMMdd-HHmmss-fff";
    private const string BackupExtension = ".bak";

    //ბექაპების სრული გზები, უახლესი პირველია. ხელით გაკეთებულ ასლებს სხვა სახელები აქვს და სიაში არ ხვდება
    public static List<string> GetBackupFiles(string resultFileName)
    {
        string fullFilePath = Path.GetFullPath(resultFileName);
        string? folderPath = Path.GetDirectoryName(fullFilePath);
        if (folderPath is null || !Directory.Exists(folderPath))
        {
            return [];
        }

        string backupFileNamePrefix = $"{Path.GetFileName(fullFilePath)}.";

        //თარიღის ფორმატის გამო სახელების დალაგება თარიღების დალაგებასაც ნიშნავს
        return
        [
            .. Directory.EnumerateFiles(folderPath, $"{backupFileNamePrefix}*{BackupExtension}")
                .Where(x => IsBackupFileName(Path.GetFileName(x), backupFileNamePrefix))
                .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static bool IsBackupFileName(string fileName, string backupFileNamePrefix)
    {
        if (fileName.Length != backupFileNamePrefix.Length + BackupDateMask.Length + BackupExtension.Length ||
            !fileName.StartsWith(backupFileNamePrefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(BackupExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return DateTime.TryParseExact(fileName.AsSpan(backupFileNamePrefix.Length, BackupDateMask.Length),
            BackupDateMask, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
