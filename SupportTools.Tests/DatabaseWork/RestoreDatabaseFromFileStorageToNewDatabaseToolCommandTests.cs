using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using LibDatabaseWork.Models;
using LibDatabaseWork.ToolCommands.RestoreDatabaseFromFileStorageToNewDatabase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SystemTools.SharedKernel;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.DatabasesManagement.Models;
using ToolsManagement.FileManagersMain;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//ფაილსაცავები დროებითი ფოლდერებია, ბაზის მენეჯერი Mock-ია
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RestoreDatabaseFromFileStorageToNewDatabaseToolCommandTests : IDisposable
{
    private const string FileName = "dl360_AppDb_Backup_Full_2026_10_27_020000_1234567.bak";
    private const string DatabaseName = "AppDbNew";
    private const string FoldersSetName = "Default";

    private readonly Mock<IDatabaseManager> _databaseManager = new();
    private readonly string _destinationFolder;
    private readonly string _exchangeFolder;
    private readonly string _localFolder;
    private readonly RecordingLogger _logger = new();
    private readonly string _rootFolder =
        Path.Combine(Path.GetTempPath(), $"RestoreFromFileStorageTests_{Guid.NewGuid():N}");
    private readonly string _sourceFolder;

    public RestoreDatabaseFromFileStorageToNewDatabaseToolCommandTests()
    {
        _localFolder = CreateFolder("local");
        _sourceFolder = CreateFolder("source");
        _destinationFolder = CreateFolder("destination");
        _exchangeFolder = CreateFolder("exchange");

        _databaseManager.Setup(x => x.IsDatabaseExists(DatabaseName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));
        _databaseManager.Setup(x => x.RestoreDatabaseFromBackup(It.IsAny<BackupFileParameters>(), DatabaseName,
                FoldersSetName, It.IsAny<EDatabaseRecoveryModel>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, true);
        }
    }

    [Fact]
    public async Task Run_AllStepsNeeded_CopiesFileEverywhereAndRestoresIt()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_sourceFolder, FileName), "backup");
        var backupFile = new BackupFileParameters(null, FileName, "prefix", ".bak", "yyyyMMddHHmmss");
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command =
            CreateCommand(backupFile, true, true, true, false);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(Path.Combine(_localFolder, FileName)));
        Assert.True(File.Exists(Path.Combine(_destinationFolder, FileName)));
        Assert.True(File.Exists(Path.Combine(_exchangeFolder, FileName)));
        _databaseManager.Verify(x => x.IsDatabaseExists(DatabaseName, It.IsAny<CancellationToken>()), Times.Once);
        _databaseManager.Verify(
            x => x.RestoreDatabaseFromBackup(backupFile, DatabaseName, FoldersSetName, EDatabaseRecoveryModel.Simple,
                _localFolder, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains((LogLevel.Information, $"Download File {FileName} from File Storage"), _logger.Messages);
        Assert.Contains((LogLevel.Information, $"Upload File {FileName} to Destination"), _logger.Messages);
        Assert.Contains((LogLevel.Information, $"Upload File {FileName} to Exchange"), _logger.Messages);
        Assert.DoesNotContain(_logger.Messages, x => x.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Run_NothingToCopy_OnlyRestores()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_sourceFolder, FileName), "backup");
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command = CreateCommand(CreateBackupFile(), false,
            false, false, false);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(Path.Combine(_localFolder, FileName)));
        Assert.False(File.Exists(Path.Combine(_destinationFolder, FileName)));
        Assert.False(File.Exists(Path.Combine(_exchangeFolder, FileName)));
        VerifyRestored(Times.Once());
        Assert.DoesNotContain(_logger.Messages, x => x.Message.Contains(FileName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Run_SkipBackupBeforeRestore_DoesNotBackUpExistingDatabase()
    {
        // Arrange
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command = CreateCommand(CreateBackupFile(), false,
            false, false, true);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.True(result);
        _databaseManager.Verify(x => x.IsDatabaseExists(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyRestored(Times.Once());
    }

    [Fact]
    public async Task Run_DownloadFails_ReturnsFalseWithoutRestore()
    {
        // Arrange
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command = CreateCommand(CreateBackupFile(), true,
            true, true, false);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.False(result);
        Assert.False(File.Exists(Path.Combine(_destinationFolder, FileName)));
        VerifyRestored(Times.Never());
        Assert.Contains((LogLevel.Error, $"Can not Download File {FileName}"), _logger.Messages);
    }

    [Fact]
    public async Task Run_UploadToDestinationFails_ReturnsFalseWithoutExchangeOrRestore()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_sourceFolder, FileName), "backup");
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command = CreateCommand(CreateBackupFile(), false,
            true, true, false);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.False(result);
        Assert.False(File.Exists(Path.Combine(_exchangeFolder, FileName)));
        VerifyRestored(Times.Never());
        Assert.Contains((LogLevel.Error, $"Can not Upload File {FileName}"), _logger.Messages);
        Assert.DoesNotContain((LogLevel.Information, $"Upload File {FileName} to Exchange"), _logger.Messages);
    }

    [Fact]
    public async Task Run_UploadToExchangeFails_ReturnsFalseWithoutRestore()
    {
        // Arrange
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command = CreateCommand(CreateBackupFile(), false,
            false, true, false);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.False(result);
        VerifyRestored(Times.Never());
        Assert.Contains((LogLevel.Information, $"Upload File {FileName} to Exchange"), _logger.Messages);
        Assert.Contains((LogLevel.Error, $"Can not Upload File {FileName}"), _logger.Messages);
    }

    [Fact]
    public async Task Run_NoExchangeFileManager_SkipsExchangeUpload()
    {
        // Arrange
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command = CreateCommand(CreateBackupFile(), false,
            false, true, false, false);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.True(result);
        VerifyRestored(Times.Once());
    }

    [Fact]
    public async Task Run_RestoreFails_ReturnsFalse()
    {
        // Arrange
        _databaseManager.Setup(x => x.RestoreDatabaseFromBackup(It.IsAny<BackupFileParameters>(), DatabaseName,
                FoldersSetName, It.IsAny<EDatabaseRecoveryModel>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("Restore", "Restore failed")));
        RestoreDatabaseFromFileStorageToNewDatabaseToolCommand command = CreateCommand(CreateBackupFile(), false,
            false, false, true);

        // Act
        bool result = await command.Run();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_KeepsParametersAndDescription()
    {
        // Arrange
        var parameters = new RestoreFromFileStorageParameters(CreateBackupParameters(false, true),
            CreateFileManager(_sourceFolder), CreateBackupFile(), false, false, false);

        // Act
        var command = new RestoreDatabaseFromFileStorageToNewDatabaseToolCommand(NullLogger.Instance, parameters,
            Mock.Of<IParametersManager>());

        // Assert
        Assert.Same(parameters, command.Par);
        Assert.Equal("Restore Database From File Storage to New Database", command.Description);
        Assert.True(parameters.CheckBeforeSave());
    }

    private static BackupFileParameters CreateBackupFile()
    {
        return new BackupFileParameters(null, FileName, "prefix", ".bak", "yyyyMMddHHmmss");
    }

    private RestoreDatabaseFromFileStorageToNewDatabaseToolCommand CreateCommand(BackupFileParameters backupFile,
        bool needDownloadFromSource, bool needUploadToDestination, bool needUploadToExchange,
        bool skipBackupBeforeRestore, bool withExchangeFileManager = true)
    {
        var parameters = new RestoreFromFileStorageParameters(
            CreateBackupParameters(skipBackupBeforeRestore, withExchangeFileManager), CreateFileManager(_sourceFolder),
            backupFile, needDownloadFromSource, needUploadToDestination, needUploadToExchange);
        return new RestoreDatabaseFromFileStorageToNewDatabaseToolCommand(_logger, parameters,
            Mock.Of<IParametersManager>());
    }

    private BaseBackupParameters CreateBackupParameters(bool skipBackupBeforeRestore, bool withExchangeFileManager)
    {
        var backupRestoreParameters = new BackupRestoreParameters(_databaseManager.Object,
            CreateFileManager(_destinationFolder), null, DatabaseName, FoldersSetName,
            new FileStorageData { FileStoragePath = _destinationFolder });

        return new BaseBackupParameters(backupRestoreParameters, EDatabaseRecoveryModel.Simple, true, ".down!",
            FileManagersFactory.CreateFileManager(false, NullLogger.Instance, _localFolder)!, null, true,
            withExchangeFileManager ? CreateFileManager(_exchangeFolder) : null, ".up!", _localFolder,
            skipBackupBeforeRestore, "PAZISI_", "yyyyMMddHHmmss", ".bak", "_FullDb_", true, true, EBackupType.Full);
    }

    private FileManager CreateFileManager(string storageFolder)
    {
        return FileManagersFactory.CreateFileManager(false, NullLogger.Instance, storageFolder, _localFolder)!;
    }

    private string CreateFolder(string name)
    {
        string folder = Path.Combine(_rootFolder, name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    //ყველა დონე ჩართულია და შეტყობინებები ფორმატირებული სახით ინახება
    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add((logLevel, formatter(state, exception)));
        }
    }

    private void VerifyRestored(Times times)
    {
        _databaseManager.Verify(
            x => x.RestoreDatabaseFromBackup(It.IsAny<BackupFileParameters>(), DatabaseName, FoldersSetName,
                EDatabaseRecoveryModel.Simple, _localFolder, It.IsAny<CancellationToken>()), times);
    }
}
