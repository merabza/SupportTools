using System;
using System.Linq;
using SupportToolsData;
using Xunit;

namespace SupportTools.Tests.Models;

public sealed class ToolNamesTests
{
    [Fact]
    public void GetProjectToolName_EveryTool_HasDistinctName()
    {
        // Act
        string[] names = [.. Enum.GetValues<EProjectTools>().Select(x => x.GetProjectToolName())];

        // Assert
        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(EProjectTools.CorrectNewDatabase, "Correct New Database")]
    [InlineData(EProjectTools.CreateDevDatabaseByMigration, "Create Dev Database By Migration")]
    [InlineData(EProjectTools.RecreateDevDatabase, "2 - Recreate Dev Database")]
    [InlineData(EProjectTools.DropDevDatabase, "Drop Dev Database")]
    [InlineData(EProjectTools.AnaliseDevDatabase, "Analise Dev Database")]
    [InlineData(EProjectTools.AnaliseProdCopyDatabase, "Analise Prod Copy Database")]
    [InlineData(EProjectTools.JetBrainsCleanupCode, "JetBrains Cleanup Code")]
    [InlineData(EProjectTools.GenerateApiRoutes, "Generate Api Routes")]
    [InlineData(EProjectTools.PairProdCopyAndDevDbObjects, "Pair ProdCopy and Dev Db Objects")]
    [InlineData(EProjectTools.TransferProdCopyToDevByPairs, "Transfer ProdCopy To Dev By Pairs")]
    public void GetProjectToolName_ReturnsName(EProjectTools tool, string expected)
    {
        Assert.Equal(expected, tool.GetProjectToolName());
    }

    [Fact]
    public void GetProjectToolName_UnknownTool_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((EProjectTools)999).GetProjectToolName());
    }

    [Fact]
    public void GetProjectServerToolName_EveryToolButPreparer_HasDistinctName()
    {
        // Act
        string[] names =
        [
            .. Enum.GetValues<EProjectServerTools>().Where(x => x != EProjectServerTools.AppSettingsPreparer)
                .Select(x => x.GetProjectServerToolName())
        ];

        // Assert
        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(EProjectServerTools.AppSettingsEncoder, "App Settings Encoder")]
    [InlineData(EProjectServerTools.AppSettingsInstaller, "App Settings Installer")]
    [InlineData(EProjectServerTools.AppSettingsUpdater, "App Settings Updater")]
    [InlineData(EProjectServerTools.DevBaseToServerCopier, "Dev Base To Server Copier")]
    [InlineData(EProjectServerTools.ServiceInstallScriptCreator, "Service Install Script Creator")]
    [InlineData(EProjectServerTools.ServiceRemoveScriptCreator, "Service Remove Script Creator")]
    [InlineData(EProjectServerTools.ServerBaseToProdCopyCopier, "Server Base To Prod Copy Copier")]
    [InlineData(EProjectServerTools.ProgPublisher, "Prog Publisher")]
    [InlineData(EProjectServerTools.ProgramInstaller, "Program Installer")]
    [InlineData(EProjectServerTools.ProgramUpdater, "Program Updater")]
    [InlineData(EProjectServerTools.ProgRemover, "Prog Remover")]
    [InlineData(EProjectServerTools.ServiceStarter, "Service Starter")]
    [InlineData(EProjectServerTools.ServiceStopper, "Service Stopper")]
    [InlineData(EProjectServerTools.VersionChecker, "Version Checker")]
    [InlineData(EProjectServerTools.ServerBaseToServerCopier, "Server Base To Server Copier")]
    [InlineData(EProjectServerTools.RestoreDatabaseFromFileStorageToNewDatabase,
        "Restore Database From File Storage to New Database")]
    public void GetProjectServerToolName_ReturnsName(EProjectServerTools tool, string expected)
    {
        Assert.Equal(expected, tool.GetProjectServerToolName());
    }

    //AppSettingsPreparer-ს სახელი არ აქვს (მენიუში არ ჩანს)
    [Fact]
    public void GetProjectServerToolName_ToolWithoutName_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EProjectServerTools.AppSettingsPreparer.GetProjectServerToolName());
    }
}
