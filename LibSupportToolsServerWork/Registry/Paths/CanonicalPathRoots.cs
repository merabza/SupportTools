using System;
using System.Collections.Generic;
using System.Linq;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;

namespace LibSupportToolsServerWork.Registry.Paths;

//კანონიკური გზების ფესვები: დისკი და პირველი ფოლდერი, მაგალითად D:\1WorkDotnet. Suggest Path Mappings თითო ფესვზე
//ლოკალურ prefix-ს ეკითხება. გზის ველების სია README §4.4-შია. URL-ები (მაგ. ftp:// ფაილსაცავები) და შეფარდებითი
//გზები Windows-ის ფესვიანი არ არის, ამიტომ არ ითვლება
public static class CanonicalPathRoots
{
    //ერთი ფესვი სხვადასხვა რეგისტრით შეიძლება იყოს ჩაწერილი (D:\ და d:\), ამიტომ შედარება რეგისტრის გარეშეა
    public static List<string> Collect(SupportToolsParameters parameters)
    {
        return
        [
            .. GetPaths(parameters).Select(GetRoot).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
        ];
    }

    //ფესვი დისკის დიდი ასოთი იწერება. დისკის გარეშე გზა და D:x ფორმის გზა (დისკის მიმდინარე ფოლდერის მიმართ
    //შეფარდებითი) ფესვს არ იძლევა
    internal static string? GetRoot(string? path)
    {
        if (path is null || path.Length < 3 || !char.IsAsciiLetter(path[0]) || path[1] != ':' ||
            path[2] is not ('\\' or '/'))
        {
            return null;
        }

        string drive = $"{char.ToUpperInvariant(path[0])}:\\";
        string? firstFolder = path[3..].Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstFolder is null ? drive : drive + firstFolder;
    }

    private static IEnumerable<string?> GetPaths(SupportToolsParameters parameters)
    {
        foreach (ProjectModel project in parameters.Projects.Values)
        {
            yield return project.ProjectFolderName;
            yield return project.SolutionFileName;
            yield return project.ProjectSecurityFolderPath;
            yield return project.MigrationStartupProjectFilePath;
            yield return project.MigrationProjectFilePath;
            yield return project.SeedProjectFilePath;
            yield return project.SeedProjectParametersFilePath;
            yield return project.DataSeederRulesByTableStartupProjectFilePath;
            yield return project.OldDataConvertorForDataSeeder;
            yield return project.ExcludesRulesParametersFilePath;
            yield return project.AppSetEnKeysJsonFileName;
            yield return project.MigrationSqlFilesFolder;
            yield return project.PrepareProdCopyDatabaseProjectFilePath;
            yield return project.PrepareProdCopyDatabaseProjectParametersFilePath;
            yield return project.PairedDbObjectsResultFileName;

            foreach (ServerInfoModel serverInfo in project.ServerInfos.Values)
            {
                yield return serverInfo.AppSettingsJsonSourceFileName;
                yield return serverInfo.AppSettingsEncodedJsonFileName;
            }
        }

        yield return parameters.AppProjectCreatorAllParameters?.ProjectsFolderPathReal;
        yield return parameters.AppProjectCreatorAllParameters?.SecretsFolderPathReal;

        foreach (FileStorageData fileStorage in parameters.FileStorages.Values)
        {
            yield return fileStorage.FileStoragePath;
        }
    }
}
