using System;

namespace SupportToolsData;

public static class ToolNames
{
    public static string GetToolName(this ETools tool)
    {
        return tool switch
        {
            ETools.CorrectNewDatabase => "Correct New Database",
            ETools.CreateDevDatabaseByMigration => "Create Dev Database By Migration",
            ETools.RecreateDevDatabase => "2 - Recreate Dev Database",
            ETools.DropDevDatabase => "Drop Dev Database",
            ETools.JetBrainsCleanupCode => "JetBrains Cleanup Code",
            ETools.JsonFromProjectDbProjectGetter => "Json From Project Db Project Getter",
            ETools.ScaffoldSeederCreator => "1 - Scaffold Seeder Creator",
            ETools.SeedData => "3 - Seed Data",
            ETools.PrepareProdCopyDatabase => "Prepare Prod Copy Database",
            ETools.AppSettingsEncoder => "App Settings Encoder",
            ETools.AppSettingsInstaller => "App Settings Installer",
            ETools.AppSettingsUpdater => "App Settings Updater",
            ETools.DevBaseToServerCopier => "Dev Base To Server Copier",
            ETools.ServiceInstallScriptCreator => "Service Install Script Creator",
            ETools.ServiceRemoveScriptCreator => "Service Remove Script Creator",
            ETools.ServerBaseToProdCopyCopier => "Server Base To Prod Copy Copier",
            ETools.ProgPublisher => "Prog Publisher",
            ETools.ProgramInstaller => "Program Installer",
            ETools.ProgramUpdater => "Program Updater",
            ETools.ProgRemover => "Prog Remover",
            ETools.ServiceStarter => "Service Starter",
            ETools.ServiceStopper => "Service Stopper",
            ETools.VersionChecker => "Version Checker",
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
        };
    }
}