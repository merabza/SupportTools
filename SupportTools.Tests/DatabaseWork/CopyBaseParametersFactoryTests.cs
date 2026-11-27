using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibDatabaseWork;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//ნამდვილი ფაბრიკებით: ფაილსაცავები დროებითი ფოლდერებია, SQL Server-ის მენეჯერი სერვერს არ უკავშირდება
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CopyBaseParametersFactoryTests : IDisposable
{
    //ToolsManagement-ის შეცდომა, როცა ფაილსაცავი ვერ შეიქმნა (წყაროსთვისაც და მიზნისთვისაც ერთი ტექსტია)
    private static readonly string StorageNotCreatedLine =
        $"[ERROR] FileStorage and sourceFileManager is Not Created{Environment.NewLine}";

    private readonly StringWriter _consoleOutput = new();

    private readonly DatabaseParameters _destination =
        CreateDatabaseParameters("Pazisi", "DestinationDb", "Destination");

    private readonly string _localFolder;
    private readonly RecordingLogger _logger = new();
    private readonly TextWriter _originalOut = Console.Out;
    private readonly SupportToolsParameters _parameters;

    private readonly string _rootFolder =
        Path.Combine(Path.GetTempPath(), $"CopyBaseParametersFactoryTests_{Guid.NewGuid():N}");

    private readonly SmartSchema _smartSchema = new() { LastPreserveCount = 3 };
    private readonly DatabaseParameters _source = CreateDatabaseParameters("Riga", "SourceDb", "Source");

    public CopyBaseParametersFactoryTests()
    {
        Console.SetOut(_consoleOutput);
        _localFolder = CreateFolder("local");

        _parameters = new SupportToolsParameters
        {
            DatabasesBackupFilesExchangeParameters =
                new DatabasesBackupFilesExchangeParameters
                {
                    LocalPath = _localFolder,
                    ExchangeFileStorageName = "Exchange",
                    ExchangeSmartSchemaName = "Reduce",
                    UploadTempExtension = ".up!"
                },
            FileStorages =
            {
                ["Source"] = new FileStorageData { FileStoragePath = CreateFolder("source") },
                ["Destination"] = new FileStorageData { FileStoragePath = CreateFolder("destination") },
                ["Exchange"] = new FileStorageData { FileStoragePath = CreateFolder("exchange") },
                ["Local"] = new FileStorageData { FileStoragePath = _localFolder }
            },
            SmartSchemas = { ["Reduce"] = _smartSchema },
            DatabaseServerConnections =
            {
                ["Riga"] = CreateSqlServerConnection(), ["Pazisi"] = CreateSqlServerConnection()
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

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task CreateCopyBaseParameters_DestinationConnectionMissing_NamesDestinationDatabase(string? value)
    {
        // Arrange
        _destination.DbConnectionName = value;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        AssertOnlyErrors(result,
            "DbConnectionName is not specified in destination database parameters (database DestinationDb)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task CreateCopyBaseParameters_DestinationDatabaseNameMissing_NamesDestination(string? value)
    {
        // Arrange
        _destination.DatabaseName = value;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        AssertOnlyErrors(result, "DatabaseName is not specified in destination database parameters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task CreateCopyBaseParameters_DestinationFoldersSetMissing_NamesDestinationDatabase(string? value)
    {
        // Arrange
        _destination.DbServerFoldersSetName = value;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        AssertOnlyErrors(result,
            "DbServerFoldersSetName is not specified in destination database parameters (database DestinationDb)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task CreateCopyBaseParameters_DestinationFileStorageMissing_NamesDestinationDatabase(string? value)
    {
        // Arrange
        _destination.FileStorageName = value;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        AssertOnlyErrors(result,
            "FileStorageName is not specified in destination database parameters (database DestinationDb)");
    }

    [Fact]
    public async Task CreateCopyBaseParameters_SourceFoldersSetMissing_NamesSourceDatabase()
    {
        // Arrange
        _source.DbServerFoldersSetName = null;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        AssertOnlyErrors(result,
            "DbServerFoldersSetName is not specified in source database parameters (database SourceDb)");
    }

    [Fact]
    public async Task CreateCopyBaseParameters_EverythingMissing_ListsEveryMissingFieldOfBothSides()
    {
        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(new DatabaseParameters(), new DatabaseParameters());

        // Assert
        AssertOnlyErrors(result, "DbConnectionName is not specified in source database parameters",
            "DatabaseName is not specified in source database parameters",
            "DbServerFoldersSetName is not specified in source database parameters",
            "FileStorageName is not specified in source database parameters",
            "DbConnectionName is not specified in destination database parameters",
            "DatabaseName is not specified in destination database parameters",
            "DbServerFoldersSetName is not specified in destination database parameters",
            "FileStorageName is not specified in destination database parameters");
    }

    [Fact]
    public async Task CreateCopyBaseParameters_ValidParameters_ReturnsBothSidesAndExchangeSettings()
    {
        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SourceDb", result.SourceBackupParameters.BackupRestoreParameters.DatabaseName);
        Assert.Equal("DestinationDb", result.DestinationBackupParameters.BackupRestoreParameters.DatabaseName);
        Assert.NotNull(result.ExchangeFileManager);
        Assert.True(result.NeedUploadToDestination);
        Assert.True(result.NeedDownloadFromExchange);
        Assert.Same(_smartSchema, result.ExchangeSmartSchema);
        Assert.Equal(".up!", result.UploadTempExtension);
        Assert.DoesNotContain(_logger.Messages, x => x.Level == LogLevel.Error);
        //არსებული ლოკალური ფოლდერი თავიდან არ იქმნება
        Assert.DoesNotContain(_logger.Messages,
            x => x.Message.StartsWith("Creating local folder", StringComparison.Ordinal));
        Assert.Equal(
            $" exchangeFileStorage - Exchange exchangeFileStorage - Exchange exchangeFileStorage - Exchange destinationFileStorage - Destination{Environment.NewLine}",
            _consoleOutput.ToString());
    }

    [Fact]
    public async Task CreateCopyBaseParameters_DestinationIsLocalFolder_DoesNotUploadToDestination()
    {
        // Arrange
        _destination.FileStorageName = "Local";

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NeedUploadToDestination);
    }

    [Fact]
    public async Task CreateCopyBaseParameters_DestinationStorageIsSourceStorage_DoesNotUploadToDestination()
    {
        // Arrange
        _destination.FileStorageName = "Source";

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.NeedUploadToDestination);
    }

    [Fact]
    public async Task CreateCopyBaseParameters_NoExchangeStorage_DoesNotDownloadFromExchange()
    {
        // Arrange
        _parameters.DatabasesBackupFilesExchangeParameters!.ExchangeFileStorageName = null;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ExchangeFileManager);
        Assert.False(result.NeedDownloadFromExchange);
    }

    [Fact]
    public async Task CreateCopyBaseParameters_ExchangeIsLocalFolder_DoesNotDownloadFromExchange()
    {
        // Arrange
        _parameters.DatabasesBackupFilesExchangeParameters!.ExchangeFileStorageName = "Local";

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ExchangeFileManager);
        Assert.False(result.NeedDownloadFromExchange);
    }

    [Fact]
    public async Task CreateCopyBaseParameters_ExchangeIsSourceStorage_DoesNotDownloadFromExchange()
    {
        // Arrange
        _parameters.DatabasesBackupFilesExchangeParameters!.ExchangeFileStorageName = "Source";

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ExchangeFileManager);
        Assert.False(result.NeedDownloadFromExchange);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task CreateCopyBaseParameters_NoExchangeSmartSchema_HasNoExchangeSchema(string? schemaName)
    {
        // Arrange
        _parameters.DatabasesBackupFilesExchangeParameters!.ExchangeSmartSchemaName = schemaName;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ExchangeSmartSchema);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task CreateCopyBaseParameters_NoUploadTempExtension_UsesDefault(string? extension)
    {
        // Arrange
        _parameters.DatabasesBackupFilesExchangeParameters!.UploadTempExtension = extension;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("up!", result.UploadTempExtension);
    }

    [Fact]
    public async Task CreateCopyBaseParameters_SameConnectionAndDatabase_ReturnsNull()
    {
        // Arrange
        _destination.DbConnectionName = "Riga";
        _destination.DatabaseName = "SourceDb";

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.Null(result);
        Assert.Contains((LogLevel.Error, "if source and destination servers are same, base names must be different"),
            _logger.Messages);
    }

    [Theory]
    [InlineData("Riga", "DestinationDb")]
    [InlineData("Pazisi", "SourceDb")]
    public async Task CreateCopyBaseParameters_OtherConnectionOrDatabase_ReturnsParameters(string connectionName,
        string databaseName)
    {
        // Arrange
        _destination.DbConnectionName = connectionName;
        _destination.DatabaseName = databaseName;

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateCopyBaseParameters_SourceBackupParametersFail_PrintsErrorAndReturnsNull()
    {
        // Arrange
        _source.FileStorageName = "Missing";

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.Null(result);
        Assert.Contains((LogLevel.Error, "File storage with name Missing not found"), _logger.Messages);
        //ფაბრიკაც (useConsole) და PrintErrorsOnConsole-იც წერს შეცდომას, მიზნის მხარე კი აღარ მუშავდება
        Assert.Equal(StorageNotCreatedLine + StorageNotCreatedLine, _consoleOutput.ToString());
    }

    [Fact]
    public async Task CreateCopyBaseParameters_DestinationBackupParametersFail_PrintsErrorAndReturnsNull()
    {
        // Arrange
        _destination.FileStorageName = "Missing";

        // Act
        CopyBaseParameters? result = await CreateCopyBaseParameters(_source, _destination);

        // Assert
        Assert.Null(result);
        Assert.Contains((LogLevel.Error, "File storage with name Missing not found"), _logger.Messages);
        //წყაროს მხარემ გაიარა, მიზნის შეცდომას კი ფაბრიკაც და PrintErrorsOnConsole-იც წერს
        Assert.Equal($" exchangeFileStorage - Exchange{StorageNotCreatedLine}{StorageNotCreatedLine}",
            _consoleOutput.ToString());
    }

    private static DatabaseParameters CreateDatabaseParameters(string connectionName, string databaseName,
        string fileStorageName)
    {
        return new DatabaseParameters
        {
            DbConnectionName = connectionName,
            DatabaseName = databaseName,
            DbServerFoldersSetName = "Default",
            FileStorageName = fileStorageName
        };
    }

    private static DatabaseServerConnectionData CreateSqlServerConnection()
    {
        return new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.SqlServer,
            ServerAddress = "localhost",
            WindowsNtIntegratedSecurity = true
        };
    }

    private Task<CopyBaseParameters?> CreateCopyBaseParameters(DatabaseParameters source,
        DatabaseParameters destination)
    {
        return CopyBaseParametersFactory.CreateCopyBaseParameters("SupportToolsTests", _logger,
            Mock.Of<IHttpClientFactory>(), source, destination, _parameters);
    }

    private void AssertOnlyErrors(CopyBaseParameters? result, params string[] expectedMessages)
    {
        Assert.Null(result);
        Assert.All(_logger.Messages, x => Assert.Equal(LogLevel.Error, x.Level));
        Assert.Equal(expectedMessages, _logger.Messages.Select(x => x.Message));
        Assert.Empty(_consoleOutput.ToString());
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
}
