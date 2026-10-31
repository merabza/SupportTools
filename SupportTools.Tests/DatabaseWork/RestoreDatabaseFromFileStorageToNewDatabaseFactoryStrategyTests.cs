using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AppCliTools.LibDataInput;
using LibDatabaseWork.ToolCommands.RestoreDatabaseFromFileStorageToNewDatabase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//შეცდომის ხაზები კონსოლზე იწერება
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategyTests : IDisposable
{
    private const string DateMask = "yyyyMMddHHmmss";
    private readonly StringWriter _consoleOutput = new();
    private readonly TextWriter _originalOut = Console.Out;

    public RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategyTests()
    {
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void GetBackupFiles_KeepsEveryFile_NameDescending()
    {
        // Arrange
        string[] fileNames =
        [
            "dl360_model_Backup_TrLog_2026_10_27_103020_6045521.trn",
            "dl360_model_Backup_TrLog_2026_10_27_163034_6837776.trn", "notes.txt", "README",
            "dl360_GeoModelProd_Backup_Full_2026_10_27_020000_1234567.BAK"
        ];

        // Act
        List<BackupFileParameters> result =
            RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy.GetBackupFiles(fileNames, DateMask);

        // Assert
        Assert.Equal(
            [
                "README", "notes.txt", "dl360_model_Backup_TrLog_2026_10_27_163034_6837776.trn",
                "dl360_model_Backup_TrLog_2026_10_27_103020_6045521.trn",
                "dl360_GeoModelProd_Backup_Full_2026_10_27_020000_1234567.BAK"
            ], result.Select(x => x.Name));
    }

    [Fact]
    public void GetBackupFiles_PrefixIsNameWithoutExtension_SuffixIsExtension()
    {
        // Act
        BackupFileParameters result = RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy
            .GetBackupFiles(["dl360_GeoModelProd_Backup_Full_2026_10_27_020000_1234567.BAK"], DateMask).Single();

        // Assert
        Assert.Equal("dl360_GeoModelProd_Backup_Full_2026_10_27_020000_1234567", result.Prefix);
        Assert.Equal(".BAK", result.Suffix);
        Assert.Equal(DateMask, result.DateMask);
        Assert.Null(result.FolderName);
    }

    [Theory]
    [InlineData("README")]
    [InlineData(".bak")]
    public void GetBackupFiles_NoExtensionOrNoName_UsesWholeName(string fileName)
    {
        // Act
        BackupFileParameters result = RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy
            .GetBackupFiles([fileName], DateMask).Single();

        // Assert
        Assert.NotEmpty(result.Prefix);
        Assert.NotEmpty(result.Suffix);
    }

    [Fact]
    public void SelectSourceFileStorageName_OffersSortedNames_WithCurrentStorageAsDefault()
    {
        // Arrange
        string? question = null;
        List<string>? offered = null;
        string? offeredDefault = null;
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy = CreateStrategy((caption, items, def) =>
        {
            question = caption;
            offered = items;
            offeredDefault = def;
            return "Ftp";
        });

        // Act
        string? result = strategy.SelectSourceFileStorageName(["Local", "Ftp", "backups"], "Local");

        // Assert
        Assert.Equal("Ftp", result);
        Assert.Equal("Restore database from which file storage?", question);
        Assert.Equal(["backups", "Ftp", "Local"], offered);
        Assert.Equal("Local", offeredDefault);
    }

    [Fact]
    public void SelectSourceFileStorageName_UnknownDefault_OffersNoDefault()
    {
        // Arrange
        string? offeredDefault = "x";
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy = CreateStrategy((_, _, def) =>
        {
            offeredDefault = def;
            return "Ftp";
        });

        // Act
        string? result = strategy.SelectSourceFileStorageName(["Ftp"], "Missing");

        // Assert
        Assert.Equal("Ftp", result);
        Assert.Null(offeredDefault);
    }

    [Theory]
    [InlineData("(None)")]
    [InlineData(null)]
    public void SelectSourceFileStorageName_NotAName_ReturnsNull(string? answer)
    {
        // Arrange
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy = CreateStrategy((_, _, _) => answer);

        // Act
        string? result = strategy.SelectSourceFileStorageName(["Ftp"], null);

        // Assert
        Assert.Null(result);
        Assert.Contains("File storage is not selected", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SelectSourceFileStorageName_NoFileStorages_ReturnsNullWithoutAsking()
    {
        // Arrange
        bool asked = false;
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy = CreateStrategy((_, _, _) =>
        {
            asked = true;
            return null;
        });

        // Act
        string? result = strategy.SelectSourceFileStorageName([], null);

        // Assert
        Assert.Null(result);
        Assert.False(asked);
        Assert.Contains("There are no file storages to restore database from", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void SelectBackupFile_ReturnsChosenFile()
    {
        // Arrange
        List<BackupFileParameters> backupFiles = RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy
            .GetBackupFiles(["A_20261001120000.bak", "A_20261015080000.bak"], DateMask);
        string? question = null;
        List<string>? offered = null;
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy = CreateStrategy((caption, items, _) =>
        {
            question = caption;
            offered = items;
            return "A_20261001120000.bak";
        });

        // Act
        BackupFileParameters? result = strategy.SelectBackupFile("Ftp", backupFiles);

        // Assert
        Assert.Same(backupFiles[1], result);
        Assert.Equal("Restore which backup file of file storage Ftp?", question);
        Assert.Equal(["A_20261015080000.bak", "A_20261001120000.bak"], offered);
    }

    [Fact]
    public void SelectBackupFile_InputEscaped_ReturnsNull()
    {
        // Arrange
        List<BackupFileParameters> backupFiles = RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy
            .GetBackupFiles(["A_20261001120000.bak"], DateMask);
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy =
            CreateStrategy((_, _, _) => throw new DataInputException("Escaped"));

        // Act
        BackupFileParameters? result = strategy.SelectBackupFile("Ftp", backupFiles);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SelectBackupFile_NoBackupFiles_ReturnsNullWithoutAsking()
    {
        // Arrange
        bool asked = false;
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy = CreateStrategy((_, _, _) =>
        {
            asked = true;
            return null;
        });

        // Act
        BackupFileParameters? result = strategy.SelectBackupFile("Ftp", []);

        // Assert
        Assert.Null(result);
        Assert.False(asked);
        Assert.Contains("File storage Ftp has no files", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateToolCommand_NoNewDatabaseParameters_ReturnsNullWithoutAsking()
    {
        // Arrange
        var serverInfo = new ServerInfoModel { ServerName = "Riga", EnvironmentName = "Prod" };
        var parametersManager = new Mock<IParametersManager>();
        parametersManager.SetupGet(x => x.Parameters).Returns(new SupportToolsParameters());
        bool asked = false;
        RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy strategy = CreateStrategy((_, _, _) =>
        {
            asked = true;
            return null;
        });

        // Act
        IToolCommand? result = await strategy.CreateToolCommand(parametersManager.Object,
            new ProjectServerToolsFactoryStrategyParameters { ProjectName = "App", ServerInfo = serverInfo });

        // Assert
        Assert.Null(result);
        Assert.False(asked);
        Assert.Contains("NewDatabaseParameters is not specified Riga|Prod", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ToolCommandName_IsEnumName()
    {
        // Act
        string name = CreateStrategy((_, _, _) => null).ToolCommandName;

        // Assert
        Assert.Equal(nameof(EProjectServerTools.RestoreDatabaseFromFileStorageToNewDatabase), name);
    }

    private static RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy CreateStrategy(
        Func<string, List<string>, string?, string?> selectFromList)
    {
        return new RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy(Mock.Of<IApplication>(),
            NullLogger.Instance, Mock.Of<IHttpClientFactory>(), selectFromList);
    }
}
