using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools.Models;
using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//every adapter of the factory through the real engine and one fake server: the main computer (Windows, canonical
//paths) seeds the empty server, a Linux computer pulls everything in its own form, and changes and deletions made on
//one computer reach the other. All values are made up
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RegistrySyncAdaptersEndToEndTests : IDisposable
{
    private const string BootstrapName = "SupportToolsServer";
    private const string GitIgnoreContent = "bin/\r\nobj/\r\n";
    private const string EditorConfigContent = "root = true\r\n[*.cs]\r\nindent_size = 4\r\n";

    //the records of the main computer, without its bootstrap ApiClient
    private const int SeededRecordCount = 21;

    private readonly RegistryAdapterTestContext _context = new();
    private readonly Computer _linux;
    private readonly Computer _main;

    public RegistrySyncAdaptersEndToEndTests()
    {
        _main = new Computer(_context.ApiClient, NewMainParameters(Path.Combine(_context.TempFolder, "main")),
            MapperTestHelpers.WindowsPathMapper());
        _linux = new Computer(_context.ApiClient, NewLinuxParameters(Path.Combine(_context.TempFolder, "linux")),
            MapperTestHelpers.LinuxPathMapper());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Seed_WhenServerIsEmpty_PushesEveryRecordAndTheNextPlanIsInSync()
    {
        // Act
        RegistrySyncReport report = await _main.Sync();
        RegistrySyncPlan nextPlan = await _main.CreatePlan();

        // Assert
        Assert.Equal(SeededRecordCount, report.Items.Count);
        Assert.All(report.Items,
            x => Assert.Equal((ERegistrySyncAction.Push, ERegistrySyncOutcome.Done), (x.PlanItem.Action, x.Outcome)));
        AssertInSyncWithEqualContent(nextPlan, SeededRecordCount);
        Assert.Empty(_main.Warnings.Items);
        Assert.Empty(_main.PathMapper.Issues);
    }

    [Fact]
    public async Task NewComputer_WhenItSyncsAfterTheSeed_PullsEverythingInItsOwnForm()
    {
        // Arrange
        await _main.Sync();

        // Act
        RegistrySyncReport report = await _linux.Sync();
        RegistrySyncPlan nextPlan = await _linux.CreatePlan();

        // Assert
        Assert.Equal(SeededRecordCount, report.Items.Count);
        Assert.All(report.Items,
            x => Assert.Equal((ERegistrySyncAction.Pull, ERegistrySyncOutcome.Done), (x.PlanItem.Action, x.Outcome)));
        AssertInSyncWithEqualContent(nextPlan, SeededRecordCount);
        Assert.Empty(_linux.PathMapper.Issues);

        SupportToolsParameters linux = _linux.Parameters;
        Assert.Equal(_main.Parameters.Environments.OrderBy(x => x.Key, StringComparer.Ordinal),
            linux.Environments.OrderBy(x => x.Key, StringComparer.Ordinal));
        Assert.Equal("/home/u/1WorkDotnet/Backups", linux.FileStorages["Exchange"].FileStoragePath);
        Assert.Equal("ftp://files.example.test/backups", linux.FileStorages["Ftp"].FileStoragePath);
        Assert.Equal("Front/app", linux.Gits["AppFront"].GitProjectFolderName);
        Assert.Equal(GitIgnoreContent,
            await File.ReadAllTextAsync(
                SupportToolsParameters.GetGitIgnoreModelFilePath(linux.FolderForGitignoreFiles!, "CSharp")));
        Assert.Equal(EditorConfigContent,
            await File.ReadAllTextAsync(
                SupportToolsParameters.GetEditorConfigPatternFilePath(linux.FolderForEditorConfigFiles!, "default")));
        Assert.True(linux.Servers["Merinson"].IsLocal);
        Assert.False(linux.Servers["PAZISI"].IsLocal);
        Assert.Equal("http://sts.linux.example.test/api/v1", linux.ApiClients[BootstrapName].Server);
        Assert.Equal("fake-agent-key", linux.ApiClients["Pc1.WebAgent"].ApiKey);
        Assert.Equal("fake-license-key", linux.MediatRLicenseKey);
        Assert.Equal("Exchange", linux.DatabasesBackupFilesExchangeParameters?.ExchangeFileStorageName);
        Assert.Null(linux.DatabasesBackupFilesExchangeParameters?.LocalPath);
        Assert.Equal("/home/u/1WorkDotnet/Projects", linux.AppProjectCreatorAllParameters?.ProjectsFolderPathReal);
        Assert.Equal(ESupportProjectType.Api,
            linux.AppProjectCreatorAllParameters?.Templates["Api"].SupportProjectType);
        Assert.Equal(EPeriodType.Month, linux.SmartSchemas["Daily"].Details[1].PeriodType);
        Assert.Equal(["Archive", "Default"], linux.DatabaseServerConnections["Dev"].DatabaseFoldersSets!.Keys);
        Assert.Null(linux.DotnetTools["dotnet-ef"].InstalledVersion);
    }

    [Fact]
    public async Task Change_WhenMadeOnOneComputer_ReachesTheOtherInItsForm()
    {
        // Arrange
        await _main.Sync();
        await _linux.Sync();
        _linux.Parameters.Environments["Production"] = "Changed on Linux";
        _linux.Parameters.FileStorages["Exchange"].FileStoragePath = "/home/u/1WorkDotnet/NewBackups";

        // Act
        RegistrySyncReport push = await _linux.Sync();
        RegistrySyncReport pull = await _main.Sync();

        // Assert
        Assert.Equal(2, push.Items.Count(x => x.PlanItem.Action == ERegistrySyncAction.Push));
        Assert.Equal(2, pull.Items.Count(x => x is
        {
            PlanItem.Action: ERegistrySyncAction.Pull, Outcome: ERegistrySyncOutcome.Done
        }));
        Assert.Equal("Changed on Linux", _main.Parameters.Environments["Production"]);
        Assert.Equal(@"D:\1WorkDotnet\NewBackups", _main.Parameters.FileStorages["Exchange"].FileStoragePath);
    }

    //a deleted template leaves the list of the other computer, but its file stays there
    [Fact]
    public async Task Delete_WhenMadeOnOneComputer_RemovesTheRecordOnTheOther()
    {
        // Arrange
        await _main.Sync();
        await _linux.Sync();
        _main.Parameters.NpmPackages.Remove("react");
        _main.Parameters.EditorConfigPatterns.Remove("default");

        // Act
        RegistrySyncReport push = await _main.Sync();
        RegistrySyncReport pull = await _linux.Sync();

        // Assert
        Assert.Equal(2, push.Items.Count(x => x is
        {
            PlanItem.Action: ERegistrySyncAction.Push, PlanItem.Change: ERegistrySyncChange.Delete,
            Outcome: ERegistrySyncOutcome.Done
        }));
        Assert.Equal(2, pull.Items.Count(x => x is
        {
            PlanItem.Action: ERegistrySyncAction.Pull, PlanItem.Change: ERegistrySyncChange.Delete,
            Outcome: ERegistrySyncOutcome.Done
        }));
        Assert.Empty(_linux.Parameters.NpmPackages);
        Assert.Empty(_linux.Parameters.EditorConfigPatterns);
        Assert.True(File.Exists(SupportToolsParameters.GetEditorConfigPatternFilePath(
            _linux.Parameters.FolderForEditorConfigFiles!, "default")));
    }

    private static void AssertInSyncWithEqualContent(RegistrySyncPlan plan, int count)
    {
        Assert.Equal(count, plan.Items.Count);
        Assert.All(plan.Items, x =>
        {
            Assert.Equal(ERegistrySyncAction.InSync, x.Action);
            Assert.Equal(x.Local?.Hash, x.Server?.Hash);
        });
    }

    private static SupportToolsParameters NewMainParameters(string folder)
    {
        string gitIgnoreFolder = Path.Combine(folder, "gitignore");
        string editorConfigFolder = Path.Combine(folder, "editorconfig");
        Directory.CreateDirectory(gitIgnoreFolder);
        Directory.CreateDirectory(editorConfigFolder);
        File.WriteAllText(SupportToolsParameters.GetGitIgnoreModelFilePath(gitIgnoreFolder, "CSharp"),
            GitIgnoreContent);
        File.WriteAllText(SupportToolsParameters.GetEditorConfigPatternFilePath(editorConfigFolder, "default"),
            EditorConfigContent);

        return new SupportToolsParameters
        {
            SupportToolsServerWebApiClientName = BootstrapName,
            CurrentMachineServerName = "PAZISI",
            FolderForGitignoreFiles = gitIgnoreFolder,
            FolderForEditorConfigFiles = editorConfigFolder,
            ServiceDescriptionSignature = "Fake signature",
            UploadTempExtension = ".up!",
            MediatRLicenseKey = "fake-license-key",
            FileStorageNameForExchange = "Exchange",
            SmartSchemaNameForExchange = "Daily",
            LocalPackageManagerWebApiClientName = "Pc1.WebAgent",
            DatabasesBackupFilesExchangeParameters =
                new DatabasesBackupFilesExchangeParameters
                {
                    ExchangeFileStorageName = "Exchange", LocalPath = @"D:\Local\Backups"
                },
            AppProjectCreatorAllParameters =
                new AppProjectCreatorAllParameters
                {
                    IndentSize = 4,
                    ProjectsFolderPathReal = @"D:\1WorkDotnet\Projects",
                    SecretsFolderPathReal = @"D:\1WorkDotnet\Secrets",
                    ProductionServerName = "PAZISI",
                    ProductionEnvironmentName = "Production",
                    DeveloperDbConnectionName = "Dev",
                    Templates =
                    {
                        ["Console With Database"] =
                            new TemplateModel
                            {
                                SupportProjectType = ESupportProjectType.Console, UseDatabase = true
                            },
                        ["Api"] =
                            new TemplateModel
                            {
                                SupportProjectType = ESupportProjectType.Api,
                                UseReact = true,
                                ReactTemplateName = "TypeScript"
                            }
                    }
                },
            Environments = { ["Production"] = "Live", ["Development"] = "" },
            RunTimes = { ["win-x64"] = "Windows", ["linux-x64"] = "Linux" },
            NpmPackages = { ["react"] = "React" },
            ReactAppTemplates = { ["TypeScript"] = "cra-template-typescript" },
            DotnetTools =
            {
                ["dotnet-ef"] = new DotnetToolData
                {
                    PackageId = "dotnet-ef", InstalledVersion = "8.0.1", CommandName = "dotnet ef"
                }
            },
            SmartSchemas =
            {
                ["Daily"] = new SmartSchema
                {
                    LastPreserveCount = 2,
                    Details =
                    [
                        new SmartSchemaDetail { PeriodType = EPeriodType.Month, PreserveCount = 12 },
                        new SmartSchemaDetail { PeriodType = EPeriodType.Day, PreserveCount = 7 }
                    ]
                }
            },
            FileStorages =
            {
                ["Exchange"] =
                    new FileStorageData { FileStoragePath = @"D:\1WorkDotnet\Backups", Password = "fake-password" },
                ["Ftp"] =
                    new FileStorageData
                    {
                        FileStoragePath = "ftp://files.example.test/backups", UserName = "fake-user"
                    }
            },
            ApiClients =
            {
                [BootstrapName] =
                    new ApiClientSettings
                    {
                        Server = "http://sts.main.example.test/api/v1", ApiKey = "fake-main-key"
                    },
                ["Pc1.WebAgent"] =
                    new ApiClientSettings { Server = "http://pc1.example.test/api", ApiKey = "fake-agent-key" }
            },
            DatabaseServerConnections =
            {
                ["Dev"] = new DatabaseServerConnectionData
                {
                    DatabaseServerProvider = EDatabaseProvider.SqlServer,
                    DbWebAgentName = "Pc1.WebAgent",
                    ServerAddress = @"(localdb)\Dev",
                    ServerPass = "fake-password",
                    DatabaseFoldersSets =
                        new Dictionary<string, DatabaseFoldersSet>
                        {
                            ["Default"] = new() { Backup = @"D:\Backup" },
                            ["Archive"] = new() { Backup = @"E:\Archive" }
                        }
                }
            },
            Servers =
            {
                ["PAZISI"] =
                    new ServerDataModel { IsLocal = true, Runtime = "win-x64", WebAgentName = "Pc1.WebAgent" },
                ["Merinson"] = new ServerDataModel { Runtime = "linux-x64", ServerSideDeployFolder = "/srv/apps" }
            },
            GitIgnorePatterns = { "CSharp" },
            Gits =
            {
                ["AppFront"] = new GitDataModel
                {
                    GitProjectAddress = "git@github.com:fake/AppFront.git",
                    GitProjectFolderName = @"Front\app",
                    GitIgnorePatternName = "CSharp"
                }
            },
            EditorConfigPatterns = { "default" }
        };
    }

    //only the machine part: its folders, its bootstrap ApiClient and its server name. The template folders do not
    //exist yet
    private static SupportToolsParameters NewLinuxParameters(string folder)
    {
        return new SupportToolsParameters
        {
            SupportToolsServerWebApiClientName = BootstrapName,
            CurrentMachineServerName = "Merinson",
            FolderForGitignoreFiles = Path.Combine(folder, "gitignore"),
            FolderForEditorConfigFiles = Path.Combine(folder, "editorconfig"),
            ApiClients =
            {
                [BootstrapName] = new ApiClientSettings
                {
                    Server = "http://sts.linux.example.test/api/v1", ApiKey = "fake-linux-key"
                }
            }
        };
    }

    //one computer: its parameters with a fake IParametersManager and an engine over all the adapters of the factory
    private sealed class Computer
    {
        private readonly RegistrySyncEngine _engine;

        public Computer(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters, PathMapper pathMapper)
        {
            Parameters = parameters;
            PathMapper = pathMapper;
            var parametersManager = new Mock<IParametersManager>();
            parametersManager.SetupGet(x => x.Parameters).Returns(parameters);
            parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _engine = new RegistrySyncEngine(
                RegistrySyncAdapterFactory.CreateAdapters(apiClient, parameters, pathMapper, Warnings),
                parametersManager.Object);
        }

        public SupportToolsParameters Parameters { get; }
        public PathMapper PathMapper { get; }
        public RegistrySyncWarnings Warnings { get; } = new();

        public async Task<RegistrySyncPlan> CreatePlan()
        {
            Result<RegistrySyncPlan> plan = await _engine.CreatePlan();
            Assert.True(plan.IsSuccess, plan.IsFailure ? plan.Error.Description : null);
            return plan.Value;
        }

        public async Task<RegistrySyncReport> Sync()
        {
            RegistrySyncPlan plan = await CreatePlan();
            RegistrySyncReport report = await _engine.Execute(plan, RegistrySyncSelection.AllNonConflicting);
            Assert.Null(report.TransportError);
            Assert.DoesNotContain(report.Items,
                x => x.Outcome is ERegistrySyncOutcome.Failed or ERegistrySyncOutcome.Conflict);
            return report;
        }
    }
}
