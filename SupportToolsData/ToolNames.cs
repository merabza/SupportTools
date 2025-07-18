using System;

namespace SupportToolsData;

public static class ToolNames
{
    public static string GetProjectToolName(this EProjectTools tool)
    {
        return tool switch
        {
            EProjectTools.CorrectNewDatabase => "Correct New Database",
            EProjectTools.CreateDevDatabaseByMigration => "Create Dev Database By Migration",
            EProjectTools.RecreateDevDatabase => "2 - Recreate Dev Database",
            EProjectTools.DropDevDatabase => "Drop Dev Database",
            EProjectTools.JetBrainsCleanupCode => "JetBrains Cleanup Code",
            EProjectTools.JsonFromProjectDbProjectGetter => "Json From Project Db Project Getter",
            EProjectTools.ScaffoldSeederCreator => "1 - Scaffold Seeder Creator",
            EProjectTools.SeedData => "3 - Seed Data",
            EProjectTools.PrepareProdCopyDatabase => "Prepare Prod Copy Database",
            //EProjectTools.AppSettingsEncoder => "App Settings Encoder",
            //EProjectTools.AppSettingsInstaller => "App Settings Installer",
            //EProjectTools.AppSettingsUpdater => "App Settings Updater",
            //EProjectTools.DevBaseToServerCopier => "Dev Base To Server Copier",
            //EProjectTools.ServiceInstallScriptCreator => "Service Install Script Creator",
            //EProjectTools.ServiceRemoveScriptCreator => "Service Remove Script Creator",
            //EProjectTools.ServerBaseToProdCopyCopier => "Server Base To Prod Copy Copier",
            //EProjectTools.ProgPublisher => "Prog Publisher",
            //EProjectTools.ProgramInstaller => "Program Installer",
            //EProjectTools.ProgramUpdater => "Program Updater",
            //EProjectTools.ProgRemover => "Prog Remover",
            //EProjectTools.ServiceStarter => "Service Starter",
            //EProjectTools.ServiceStopper => "Service Stopper",
            //EProjectTools.VersionChecker => "Version Checker",
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
        };
    }
    public static string GetProjectServerToolName(this EProjectServerTools tool)
    {
        return tool switch
        {
            //EProjectTools.CorrectNewDatabase => "Correct New Database",
            //EProjectTools.CreateDevDatabaseByMigration => "Create Dev Database By Migration",
            //EProjectTools.RecreateDevDatabase => "2 - Recreate Dev Database",
            //EProjectTools.DropDevDatabase => "Drop Dev Database",
            //EProjectTools.JetBrainsCleanupCode => "JetBrains Cleanup Code",
            //EProjectTools.JsonFromProjectDbProjectGetter => "Json From Project Db Project Getter",
            //EProjectTools.ScaffoldSeederCreator => "1 - Scaffold Seeder Creator",
            //EProjectTools.SeedData => "3 - Seed Data",
            //EProjectTools.PrepareProdCopyDatabase => "Prepare Prod Copy Database",
            EProjectServerTools.AppSettingsEncoder => "App Settings Encoder",
            EProjectServerTools.AppSettingsInstaller => "App Settings Installer",
            EProjectServerTools.AppSettingsUpdater => "App Settings Updater",
            EProjectServerTools.DevBaseToServerCopier => "Dev Base To Server Copier",
            EProjectServerTools.ServiceInstallScriptCreator => "Service Install Script Creator",
            EProjectServerTools.ServiceRemoveScriptCreator => "Service Remove Script Creator",
            EProjectServerTools.ServerBaseToProdCopyCopier => "Server Base To Prod Copy Copier",
            EProjectServerTools.ProgPublisher => "Prog Publisher",
            EProjectServerTools.ProgramInstaller => "Program Installer",
            EProjectServerTools.ProgramUpdater => "Program Updater",
            EProjectServerTools.ProgRemover => "Prog Remover",
            EProjectServerTools.ServiceStarter => "Service Starter",
            EProjectServerTools.ServiceStopper => "Service Stopper",
            EProjectServerTools.VersionChecker => "Version Checker",
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
        };
    }
}