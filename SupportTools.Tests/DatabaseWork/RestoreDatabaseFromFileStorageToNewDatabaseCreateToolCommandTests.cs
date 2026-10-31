using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using LibDatabaseWork.Models;
using LibDatabaseWork.ToolCommands.RestoreDatabaseFromFileStorageToNewDatabase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//CreateToolCommand ნამდვილი ფაბრიკებით: ფაილსაცავები დროებითი ფოლდერებია, SQL Server-ის მენეჯერი სერვერს არ უკავშირდება
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RestoreDatabaseFromFileStorageToNewDatabaseCreateToolCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new();
    private readonly string _localFolder;
    private readonly TextWriter _originalOut = Console.Out;
    private readonly SupportToolsParameters _parameters;
    private readonly List<(string Caption, List<string> Items, string? Default)> _questions = [];
    private readonly string _rootFolder =
        Path.Combine(Path.GetTempPath(), $"RestoreFromFileStorageCreateTests_{Guid.NewGuid():N}");
    private readonly ServerInfoModel _serverInfo;
    private readonly string _sourceFolder;

    public RestoreDatabaseFromFileStorageToNewDatabaseCreateToolCommandTests()
    {
        Console.SetOut(_consoleOutput);
        _localFolder = CreateFolder("local");
        _sourceFolder = CreateFolder("source");

        _parameters = new SupportToolsParameters
        {
            DatabasesBackupFilesExchangeParameters = new DatabasesBackupFilesExchangeParameters
            {
                LocalPath = _localFolder, ExchangeFileStorageName = "Exchange"
            },
            FileStorages =
            {
                ["Source"] = new FileStorageData { FileStoragePath = _sourceFolder },
                ["Destination"] = new FileStorageData { FileStoragePath = CreateFolder("destination") },
                ["Exchange"] = new FileStorageData { FileStoragePath = CreateFolder("exchange") },
                ["Local"] = new FileStorageData { FileStoragePath = _localFolder },
                ["Broken"] = new FileStorageData()
            },
            DatabaseServerConnections =
            {
                ["Sql"] = new DatabaseServerConnectionData
                {
                    DatabaseServerProvider = EDatabaseProvider.SqlServer,
                    ServerAddress = "localhost",
                    WindowsNtIntegratedSecurity = true
                }
            }
        };

        _serverInfo = new ServerInfoModel
        {
            ServerName = "Riga",
            EnvironmentName = "Prod",
            CurrentDatabaseParameters = new DatabaseParameters { FileStorageName = "Source" },
            NewDatabaseParameters = new DatabaseParameters
            {
                DbConnectionName = "Sql",
                DatabaseName = "AppDbNew",
                DbServerFoldersSetName = "Default",
                FileStorageName = "Destination"
            }
        };
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _consoleOutput.Dispose();
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, true);
        }
    }

    [Fact]
    public async Task CreateToolCommand_StorageAndFileSelected_CreatesCommandThatCopiesEverywhere()
    {
        // Arrange
        await WriteSourceFiles("a.bak", "b.trn");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Source", "a.bak");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("a.bak", result.BackupFile.Name);
        Assert.Equal("a", result.BackupFile.Prefix);
        Assert.Equal(".bak", result.BackupFile.Suffix);
        Assert.Equal(DatabaseParameters.DefaultDateMask, result.BackupFile.DateMask);
        Assert.True(result.NeedDownloadFromSource);
        Assert.True(result.NeedUploadToDestination);
        Assert.True(result.NeedUploadToExchange);
        Assert.Equal("AppDbNew", result.DestinationBackupParameters.BackupRestoreParameters.DatabaseName);
        Assert.True(result.SourceFileManager.FileExists(null, "b.trn"));

        Assert.Equal(2, _questions.Count);
        Assert.Equal("Restore database from which file storage?", _questions[0].Caption);
        Assert.Equal(["Broken", "Destination", "Exchange", "Local", "Source"], _questions[0].Items);
        Assert.Equal("Source", _questions[0].Default);
        Assert.Equal("Restore which backup file of file storage Source?", _questions[1].Caption);
        Assert.Equal(["b.trn", "a.bak"], _questions[1].Items);
        Assert.Null(_questions[1].Default);
        Assert.Contains($" exchangeFileStorage - Exchange{Environment.NewLine}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.Contains("File a.bak of file storage Source will be restored to database AppDbNew of server Riga|Prod",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateToolCommand_SourceIsDestinationStorage_DoesNotUploadToDestination()
    {
        // Arrange
        _parameters.FileStorages["Destination"].FileStoragePath = _sourceFolder;
        await WriteSourceFiles("a.bak");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Destination", "a.bak");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.NeedDownloadFromSource);
        Assert.False(result.NeedUploadToDestination);
        Assert.True(result.NeedUploadToExchange);
    }

    [Fact]
    public async Task CreateToolCommand_SourceIsExchangeStorage_DoesNotUploadToExchange()
    {
        // Arrange
        _parameters.FileStorages["Exchange"].FileStoragePath = _sourceFolder;
        await WriteSourceFiles("a.bak");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Exchange", "a.bak");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.NeedUploadToDestination);
        Assert.False(result.NeedUploadToExchange);
    }

    [Fact]
    public async Task CreateToolCommand_SourceIsLocalFolder_DoesNotDownload()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_localFolder, "a.bak"), "backup");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Local", "a.bak");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NeedDownloadFromSource);
        Assert.True(result.NeedUploadToDestination);
    }

    [Fact]
    public async Task CreateToolCommand_DestinationIsLocalFolder_DoesNotUploadToDestination()
    {
        // Arrange
        _serverInfo.NewDatabaseParameters!.FileStorageName = "Local";
        await WriteSourceFiles("a.bak");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Source", "a.bak");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NeedUploadToDestination);
    }

    [Fact]
    public async Task CreateToolCommand_NoExchangeStorage_DoesNotUploadToExchange()
    {
        // Arrange
        _parameters.DatabasesBackupFilesExchangeParameters!.ExchangeFileStorageName = null;
        await WriteSourceFiles("a.bak");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Source", "a.bak");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NeedUploadToExchange);
    }

    [Fact]
    public async Task CreateToolCommand_NoCurrentDatabaseStorage_OffersNoDefaultStorage()
    {
        // Arrange
        _serverInfo.CurrentDatabaseParameters!.FileStorageName = null;
        await WriteSourceFiles("a.bak");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Source", "a.bak");

        // Assert
        Assert.NotNull(result);
        Assert.Null(_questions[0].Default);
    }

    [Fact]
    public async Task CreateToolCommand_DestinationParametersInvalid_ReturnsNullWithoutAsking()
    {
        // Arrange
        _parameters.DatabasesBackupFilesExchangeParameters!.LocalPath = null;

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Source", "a.bak");

        // Assert
        Assert.Null(result);
        Assert.Empty(_questions);
        //ფაბრიკაც (useConsole) და PrintErrorsOnConsole-იც წერს შეცდომას
        string output = _consoleOutput.ToString();
        const string error = "localPath is not specified in databasesBackupFilesExchangeParameter";
        Assert.Equal(2, output.Split(error).Length - 1);
    }

    [Fact]
    public async Task CreateToolCommand_StorageNotSelected_ReturnsNull()
    {
        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters(null, "a.bak");

        // Assert
        Assert.Null(result);
        Assert.Single(_questions);
        Assert.DoesNotContain("File manager for file storage", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateToolCommand_StorageFolderCannotBeCreated_ReturnsNull()
    {
        // Arrange
        //ფოლდერი ფაილის შიგნით ვერ შეიქმნება, ამიტომ ფაილმენეჯერი არ იქმნება
        string blockingFile = Path.Combine(_rootFolder, "blocking-file");
        await File.WriteAllTextAsync(blockingFile, "file");
        _parameters.FileStorages["Broken"].FileStoragePath = Path.Combine(blockingFile, "storage");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Broken", "a.bak");

        // Assert
        Assert.Null(result);
        Assert.Single(_questions);
        Assert.Contains("File manager for file storage Broken is not created", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateToolCommand_EmptyStorage_ReturnsNull()
    {
        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Source", "a.bak");

        // Assert
        Assert.Null(result);
        Assert.Contains("File storage Source has no files", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateToolCommand_FileNotSelected_ReturnsNull()
    {
        // Arrange
        await WriteSourceFiles("a.bak");

        // Act
        RestoreFromFileStorageParameters? result = await CreateParameters("Source", null);

        // Assert
        Assert.Null(result);
        Assert.Equal(2, _questions.Count);
        Assert.Contains("Backup file is not selected", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void PublicConstructor_SelectsFromConsoleMenu()
    {
        // Arrange
        var strategy = new RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy(Mock.Of<IApplication>(),
            NullLogger<RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy>.Instance,
            Mock.Of<IHttpClientFactory>());

        // Act
        Exception? exception = Record.Exception(() => strategy.SelectSourceFileStorageName(["Ftp", "Local"], "Local"));

        // Assert
        //სატესტო ჰოსტში კონსოლიდან წაკითხვა ვერ ხერხდება; მოთხოვნა უკვე დაწერილია
        Assert.NotNull(exception);
        string output = _consoleOutput.ToString();
        Assert.Contains("Select Restore database from which file storage? [Local]: ", output, StringComparison.Ordinal);
    }

    private async Task<RestoreFromFileStorageParameters?> CreateParameters(string? fileStorageName,
        string? fileName)
    {
        var parametersManager = new Mock<IParametersManager>();
        parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        var strategy = new RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy(Mock.Of<IApplication>(),
            NullLogger.Instance, Mock.Of<IHttpClientFactory>(), (caption, items, defaultValue) =>
            {
                _questions.Add((caption, items, defaultValue));
                return _questions.Count == 1 ? fileStorageName : fileName;
            });

        IToolCommand? command = await strategy.CreateToolCommand(parametersManager.Object,
            new ProjectServerToolsFactoryStrategyParameters { ProjectName = "App", ServerInfo = _serverInfo });

        return (RestoreFromFileStorageParameters?)((RestoreDatabaseFromFileStorageToNewDatabaseToolCommand?)command)
            ?.Par;
    }

    private async Task WriteSourceFiles(params string[] fileNames)
    {
        foreach (string fileName in fileNames)
        {
            await File.WriteAllTextAsync(Path.Combine(_sourceFolder, fileName), fileName);
        }
    }

    private string CreateFolder(string name)
    {
        string folder = Path.Combine(_rootFolder, name);
        Directory.CreateDirectory(folder);
        return folder;
    }
}
