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
            EProjectTools.AnaliseDevDatabase => "Analise Dev Database",
            EProjectTools.AnaliseProdCopyDatabase => "Analise Prod Copy Database",
            EProjectTools.JetBrainsCleanupCode => "JetBrains Cleanup Code",
            EProjectTools.JsonFromProjectDbProjectGetter => "Json From Project Db Project Getter",
            EProjectTools.ScaffoldSeederCreator => "1 - Scaffold Seeder Creator",
            EProjectTools.SeedData => "3 - Seed Data",
            EProjectTools.PrepareProdCopyDatabase => "Prepare Prod Copy Database",
            EProjectTools.GenerateApiRoutes => "Generate Api Routes",
            EProjectTools.PairProdCopyAndDevDbObjects => "Pair ProdCopy and Dev Db Objects",
            EProjectTools.TransferProdCopyToDevByPairs => "Transfer ProdCopy To Dev By Pairs",
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
        };
    }

    public static string GetProjectServerToolName(this EProjectServerTools tool)
    {
        return tool switch
        {
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
