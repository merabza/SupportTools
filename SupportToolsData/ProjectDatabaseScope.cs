using System;
using SupportToolsData.Models;

namespace SupportToolsData;

//რედაქტორებსა და ინსტრუმენტებში რა ჩანს პროექტის ტიპისა და UsesDatabase-ის მიხედვით: IsDbTools ტიპის პროექტს მხოლოდ
//მონაცემთა ბაზასთან დაკავშირებული ინსტრუმენტები აქვს, UsesDatabase-გამორთულ პროექტს კი ბაზასთან დაკავშირებული არაფერი
public static class ProjectDatabaseScope
{
    public static bool IsDbTools(this ProjectModel project)
    {
        return project.ProjectType == EProjectType.IsDbTools;
    }

    //IsDbTools ტიპის პროექტი ბაზას ყოველთვის იყენებს, UsesDatabase-ის მიუხედავად
    public static bool ShowsDatabase(this ProjectModel project)
    {
        return project.IsDbTools() || project.UsesDatabase;
    }

    public static bool IsToolVisible(this ProjectModel project, EProjectTools tool)
    {
        return project.IsDbTools() ? tool.IsDatabaseTool() : project.UsesDatabase || !tool.IsDatabaseTool();
    }

    public static bool IsToolVisible(this ProjectModel project, EProjectServerTools tool)
    {
        return project.IsDbTools() ? tool.IsDatabaseTool() : project.UsesDatabase || !tool.IsDatabaseTool();
    }

    public static bool IsDatabaseTool(this EProjectTools tool)
    {
        return tool switch
        {
            EProjectTools.CorrectNewDatabase or EProjectTools.CreateDevDatabaseByMigration
                or EProjectTools.RecreateDevDatabase or EProjectTools.DropDevDatabase
                or EProjectTools.AnaliseDevDatabase or EProjectTools.AnaliseProdCopyDatabase
                or EProjectTools.PairProdCopyAndDevDbObjects or EProjectTools.TransferProdCopyToDevByPairs => true,
            EProjectTools.JetBrainsCleanupCode or EProjectTools.GenerateApiRoutes => false,
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
        };
    }

    public static bool IsDatabaseTool(this EProjectServerTools tool)
    {
        return tool switch
        {
            EProjectServerTools.DevBaseToServerCopier or EProjectServerTools.ServerBaseToProdCopyCopier
                or EProjectServerTools.ServerBaseToServerCopier => true,
            EProjectServerTools.AppSettingsEncoder or EProjectServerTools.AppSettingsPreparer
                or EProjectServerTools.AppSettingsInstaller or EProjectServerTools.AppSettingsUpdater
                or EProjectServerTools.ServiceInstallScriptCreator or EProjectServerTools.ServiceRemoveScriptCreator
                or EProjectServerTools.ProgPublisher or EProjectServerTools.ProgramInstaller
                or EProjectServerTools.ProgramUpdater or EProjectServerTools.ProgRemover
                or EProjectServerTools.ServiceStarter or EProjectServerTools.ServiceStopper
                or EProjectServerTools.VersionChecker => false,
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
        };
    }
}
