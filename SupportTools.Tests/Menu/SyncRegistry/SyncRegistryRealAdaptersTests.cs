using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibSupportToolsServerWork.Registry.Adapters;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportTools.Menu.SyncRegistry;
using SupportTools.Tests.Registry.Adapters;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Menu.SyncRegistry;

//the command with every real adapter of the factory and the fake server (FakeSupportToolsServer): the requests really
//go through SupportToolsServerApiClient. The main computer holds secrets, and so do the records that the server
//already has. All values are made up
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncRegistryRealAdaptersTests : IDisposable
{
    private const string BootstrapName = "SupportToolsServer";
    private const string BootstrapApiKey = "fake-bootstrap-key-0001";
    private const string AgentApiKey = "fake-agent-key-0002";
    private const string StoragePassword = "fake-storage-password-0005";
    private const string KeyGuidPart = "fake-key-guid-part-0012";
    private const string GitIgnoreContent = "fake gitignore content 0014";
    private const string ApiClients = "apiclients";
    private const string Projects = "projects";

    //every secret of both sides, the bootstrap API key among them
    private static readonly string[] Secrets =
    [
        BootstrapApiKey, AgentApiKey, "fake-server-agent-key-0003", "fake-storage-user-0004", StoragePassword,
        "fake-server-storage-password-0006", "fake-db-user-0007", "fake-db-password-0008",
        "fake-server-db-password-0009", "fake-license-key-0010", "fake-server-license-key-0011", KeyGuidPart,
        "fake-server-key-guid-part-0013", GitIgnoreContent, "fake server gitignore content 0015"
    ];

    private readonly Queue<string> _actions = new();
    private readonly RegistryAdapterTestContext _context = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    //the conflicts whose differences were shown; the second question about a conflict resolves it as Local
    private readonly HashSet<string> _shownDifferences = new(StringComparer.Ordinal);

    public SyncRegistryRealAdaptersTests()
    {
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_context.Server, false));
        _parametersManager.SetupGet(x => x.Parameters).Returns(_context.Parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
        FillMainComputer();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    //seed of an empty server: every record goes as Push(Add), with progress, and the next sync finds nothing to do
    [Fact]
    public async Task RunBody_WhenServerIsEmpty_SeedsItWithProgressAndTheNextSyncIsInSync()
    {
        // Act
        bool result = await Run("Apply");
        bool nextResult = await Run();

        // Assert
        Assert.True(result);
        Assert.True(nextResult);
        Assert.Equal(AgentApiKey, _context.Server.Get<StsApiClientDataModel>(ApiClients, "Pc1.WebAgent")?.ApiKey);
        Assert.Equal(KeyGuidPart, _context.Server.Get<StsProjectDataModel>(Projects, "AppFake")?.KeyGuidPart);
        Assert.DoesNotContain(_context.Server.Records(ApiClients).Keys, x => x == BootstrapName);
        string console = _context.ConsoleOutput;
        Assert.Contains("Sending 8 records to SupportToolsServer...", console, StringComparison.Ordinal);
        Assert.Contains("1/8 add Environments/Production: done", console, StringComparison.Ordinal);
        Assert.Contains("8/8 add Projects/AppFake: done", console, StringComparison.Ordinal);
        Assert.Contains("Sync result: done 8", console, StringComparison.Ordinal);
        Assert.Contains("The local registry and SupportToolsServer are in sync", console, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenDryRunIsChosen_OnlyReadsTheServer()
    {
        // Arrange
        StoreConflictingServerRecords();

        // Act
        bool result = await Run("Dry run", "Cancel");

        // Assert
        Assert.True(result);
        Assert.All(_context.Server.Requests, x => Assert.StartsWith("GET ", x, StringComparison.Ordinal));
        Assert.Equal(1, _context.Server.VersionOf(ApiClients, "Pc1.WebAgent"));
        Assert.Empty(_context.Server.Records("environments"));
        Assert.Contains("Dry run: Apply would send 2 records to SupportToolsServer, nothing is sent now",
            _context.ConsoleOutput, StringComparison.Ordinal);
    }

    //the details, the differences of every conflict, the dry run, the progress and the report: no secret of either
    //side reaches the console, while the differences show that the secrets differ
    [Fact]
    public async Task RunBody_WhenRecordsHoldSecrets_NeverWritesThemToTheConsole()
    {
        // Arrange
        StoreConflictingServerRecords();

        // Act
        bool result = await Run("Show details", "Resolve conflicts", "Dry run", "Apply");

        // Assert
        Assert.True(result);
        Assert.Equal(6, _shownDifferences.Count);
        Assert.Equal(AgentApiKey, _context.Server.Get<StsApiClientDataModel>(ApiClients, "Pc1.WebAgent")?.ApiKey);
        Assert.Equal(StoragePassword,
            _context.Server.Get<StsFileStorageDataModel>("filestorages", "Exchange")?.Password);
        string console = _context.ConsoleOutput;
        Assert.Contains("ApiClients/Pc1.WebAgent: first sync, differs here and on the server (ApiKey)", console,
            StringComparison.Ordinal);
        Assert.Contains("  KeyGuidPart", console, StringComparison.Ordinal);
        Assert.Contains($"    local:  {RegistryRecordDiff.HiddenValue}", console, StringComparison.Ordinal);
        Assert.All(Secrets, secret => Assert.DoesNotContain(secret, console, StringComparison.Ordinal));
    }

    //a stored file (C6) that differs on the server: the details, the differences, the dry run, the progress and the
    //report show its size and the beginning of its hash, never the content of either side or a whole hash
    [Fact]
    public async Task RunBody_WhenStoredFilesDiffer_ShowsOnlyTheirSizeAndTheBeginningOfTheirHash()
    {
        // Arrange
        const string localContent = "{\"ConnectionString\":\"fake-file-secret-0016\"}";
        const string serverContent = "{\"ConnectionString\":\"fake-server-file-secret-0017\"}";
        string path = Path.Combine(_context.TempFolder, "secrets", "AppFake", "appsettings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, localContent);
        _context.Parameters.Projects["AppFake"].ServerInfos["PAZISI|Production"] = new ServerInfoModel
        {
            ServerName = "PAZISI", EnvironmentName = "Production", AppSettingsJsonSourceFileName = path
        };
        _context.Server.StoreFile(path, serverContent, 1);
        string localHash = FakeSupportToolsServer.Sha256Of(localContent);
        string serverHash = FakeSupportToolsServer.Sha256Of(serverContent);

        // Act
        bool result = await Run("Show details", "Resolve conflicts", "Dry run", "Apply");

        // Assert
        Assert.True(result);
        Assert.Equal(localContent, _context.Server.FileContent(path));
        string console = _context.ConsoleOutput;
        Assert.Contains($"StoredFiles/{path}: first sync, differs here and on the server (Length, Sha256)", console,
            StringComparison.Ordinal);
        Assert.Contains($"    local:  {localHash[..8]}...", console, StringComparison.Ordinal);
        Assert.Contains($"    server: {serverHash[..8]}...", console, StringComparison.Ordinal);
        string[] secrets =
        [
            localContent, serverContent, "fake-file-secret-0016", "fake-server-file-secret-0017", localHash, serverHash
        ];
        Assert.All(secrets, secret => Assert.DoesNotContain(secret, console, StringComparison.Ordinal));
    }

    private async Task<bool> Run(params string[] actions)
    {
        foreach (string action in actions)
        {
            _actions.Enqueue(action);
        }

        var sut = new SyncRegistryCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _parametersManager.Object, InputIdFromMenuList, RegistrySyncAdapterFactory.CreateAdapters);
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);
        Assert.Empty(_actions);
        return result;
    }

    private int InputIdFromMenuList(string fieldName, CliMenuSet menuSet)
    {
        string answer = _actions.Count > 0 ? _actions.Peek() : string.Empty;
        if (fieldName.StartsWith("resolution of ", StringComparison.Ordinal))
        {
            answer = _shownDifferences.Add(fieldName) ? "Show differences" : "Local";
        }
        else
        {
            _actions.Dequeue();
        }

        return CliMenuTestAccess.GetMenuItems(menuSet).FindIndex(x => x.MenuItemName == answer);
    }

    private void FillMainComputer()
    {
        string gitIgnoreFolder = Path.Combine(_context.TempFolder, "gitignore");
        Directory.CreateDirectory(gitIgnoreFolder);
        File.WriteAllText(SupportToolsParameters.GetGitIgnoreModelFilePath(gitIgnoreFolder, "CSharp"),
            GitIgnoreContent);

        SupportToolsParameters parameters = _context.Parameters;
        parameters.SupportToolsServerWebApiClientName = BootstrapName;
        parameters.FolderForGitignoreFiles = gitIgnoreFolder;
        parameters.MediatRLicenseKey = "fake-license-key-0010";
        parameters.Environments["Production"] = "Live";
        parameters.NpmPackages["react"] = "React";
        parameters.ApiClients[BootstrapName] =
            new ApiClientSettings { Server = FakeSupportToolsServer.Address, ApiKey = BootstrapApiKey };
        parameters.ApiClients["Pc1.WebAgent"] =
            new ApiClientSettings { Server = "http://pc1.example.test/api", ApiKey = AgentApiKey };
        parameters.FileStorages["Exchange"] = new FileStorageData
        {
            FileStoragePath = @"D:\1WorkDotnet\Backups",
            UserName = "fake-storage-user-0004",
            Password = StoragePassword
        };
        parameters.DatabaseServerConnections["Dev"] = new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.SqlServer,
            ServerAddress = @"(localdb)\Dev",
            ServerUser = "fake-db-user-0007",
            ServerPass = "fake-db-password-0008"
        };
        parameters.GitIgnorePatterns.Add("CSharp");
        parameters.Projects["AppFake"] = new ProjectModel
        {
            ProjectType = EProjectType.IsService,
            SolutionFileName = @"D:\1WorkDotnet\AppFake\AppFake.slnx",
            KeyGuidPart = KeyGuidPart
        };
    }

    //the server already has six of the records, each with other secrets: six first sync conflicts
    private void StoreConflictingServerRecords()
    {
        FakeSupportToolsServer server = _context.Server;
        server.Store(ApiClients, "Pc1.WebAgent",
            new StsApiClientDataModel
            {
                Name = "Pc1.WebAgent", Server = "http://pc1.example.test/api", ApiKey = "fake-server-agent-key-0003"
            }, 1);
        server.Store("filestorages", "Exchange",
            new StsFileStorageDataModel
            {
                Name = "Exchange",
                FileStoragePath = @"D:\1WorkDotnet\Backups",
                UserName = "fake-storage-user-0004",
                Password = "fake-server-storage-password-0006"
            }, 1);
        server.Store("databaseserverconnections", "Dev",
            new StsDatabaseServerConnectionDataModel
            {
                Name = "Dev",
                DatabaseServerProvider = nameof(EDatabaseProvider.SqlServer),
                ServerAddress = @"(localdb)\Dev",
                ServerUser = "fake-db-user-0007",
                ServerPass = "fake-server-db-password-0009"
            }, 1);
        server.Store(FakeSupportToolsServer.GlobalSettings, string.Empty,
            new StsGlobalSettingsDataModel { MediatRLicenseKey = "fake-server-license-key-0011" }, 1);
        server.Store(FakeSupportToolsServer.GitIgnoreFileTypes, "CSharp",
            new StsGitIgnoreFileTypeDataModel
            {
                Id = Guid.NewGuid(), Name = "CSharp", Content = "fake server gitignore content 0015"
            }, 1);
        server.Store(Projects, "AppFake",
            new StsProjectDataModel
            {
                Name = "AppFake",
                ProjectType = nameof(EProjectType.IsService),
                SolutionFileName = @"D:\1WorkDotnet\AppFake\AppFake.slnx",
                KeyGuidPart = "fake-server-key-guid-part-0013"
            }, 1);
    }
}
