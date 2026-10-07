using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the stored files (C6) through every adapter of the factory, the real engine and one fake server: the main computer
//(Windows) seeds its secret files, a Linux computer takes them in its own form in the same sync as the project that
//points to them, and changes, missing files and deletions follow the user's choices. D:\1WorkSecurity is a temp
//folder on both computers. All contents are made up
[Collection(ConsoleCaptureCollection.Name)]
public sealed class StoredFilesSyncEndToEndTests : IDisposable
{
    private const string CanonicalRoot = @"D:\1WorkSecurity";
    private const string AppSettings = @"D:\1WorkSecurity\AppFake\PAZISI\Production\appsettings.json";
    private const string Paired = @"D:\1WorkSecurity\AppFake\Paired.json";
    private const string AppSettingsContent = "{\"ConnectionStrings\":{\"Main\":\"fake-connection-0001\"}}\r\n";
    private const string PairedContent = "{\"Pairs\":[]}\r\n";
    private const string ServerInfoKey = "PAZISI|Production";

    private readonly RegistryAdapterTestContext _context = new();
    private readonly Computer _linux;
    private readonly Computer _main;

    public StoredFilesSyncEndToEndTests()
    {
        string mainRoot = Path.Combine(_context.TempFolder, "main", "1WorkSecurity");
        string linuxRoot = $"{_context.TempFolder.Replace('\\', '/')}/linux/1WorkSecurity";
        var mainPathMapper =
            new PathMapper([new PathMappingModel { CanonicalPrefix = CanonicalRoot, LocalPrefix = mainRoot }], '\\');
        _main = new Computer(_context.ApiClient, NewParameters(Path.Combine(_context.TempFolder, "main")),
            mainPathMapper);
        _linux = new Computer(_context.ApiClient, NewParameters(Path.Combine(_context.TempFolder, "linux")),
            new PathMapper([new PathMappingModel { CanonicalPrefix = CanonicalRoot, LocalPrefix = linuxRoot }], '/'));

        //the main computer's appsettings.json has a BOM, as Visual Studio writes it
        WriteFile(_main.LocalPath(AppSettings), AppSettingsContent, true);
        WriteFile(_main.LocalPath(Paired), PairedContent, false);
        _main.Parameters.Projects["AppFake"] = new ProjectModel
        {
            ProjectType = EProjectType.IsService,
            PairedDbObjectsResultFileName = _main.LocalPath(Paired),
            ServerInfos =
            {
                [ServerInfoKey] = new ServerInfoModel
                {
                    ServerName = "PAZISI",
                    EnvironmentName = "Production",
                    AppSettingsJsonSourceFileName = _main.LocalPath(AppSettings)
                }
            }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Seed_WhenServerIsEmpty_SendsTheFilesWithTheProject()
    {
        // Act
        RegistrySyncReport report = await _main.Sync();
        RegistrySyncPlan nextPlan = await _main.CreatePlan();

        // Assert
        Assert.Equal([Paired, AppSettings, "AppFake"],
            report.Items.Where(x => x.Outcome == ERegistrySyncOutcome.Done).Select(x => x.PlanItem.Key));
        Assert.Equal(AppSettingsContent, _context.Server.FileContent(AppSettings));
        Assert.Equal(PairedContent, _context.Server.FileContent(Paired));
        AssertInSync(nextPlan);
        Assert.Empty(_main.Warnings.Items);
    }

    //the second step of a new computer (README §1): one sync brings the project and the files that it points to
    [Fact]
    public async Task NewComputer_WhenItSyncsAfterTheSeed_TakesTheFilesInItsFormInTheSameSync()
    {
        // Arrange
        await _main.Sync();

        // Act
        RegistrySyncReport report = await _linux.Sync();
        RegistrySyncPlan nextPlan = await _linux.CreatePlan();

        // Assert
        Assert.Equal(3, report.Items.Count(x => x is
        {
            PlanItem.Action: ERegistrySyncAction.Pull, Outcome: ERegistrySyncOutcome.Done
        }));
        string linuxAppSettings = _linux.LocalPath(AppSettings);
        Assert.Equal(linuxAppSettings,
            _linux.Parameters.Projects["AppFake"].ServerInfos[ServerInfoKey].AppSettingsJsonSourceFileName);
        Assert.Equal(Encoding.UTF8.GetBytes(AppSettingsContent), await File.ReadAllBytesAsync(linuxAppSettings));
        Assert.Equal(PairedContent, await File.ReadAllTextAsync(_linux.LocalPath(Paired)));
        AssertInSync(nextPlan);
        AssertInSync(await _main.CreatePlan());
        Assert.Empty(_linux.Warnings.Items);
        Assert.Empty(_linux.PathMapper.Issues);
    }

    //the main computer keeps the BOM of its file
    [Fact]
    public async Task Change_WhenAFileChangesOnOneComputer_ReachesTheOther()
    {
        // Arrange
        await _main.Sync();
        await _linux.Sync();
        const string changed = "{\"ConnectionStrings\":{\"Main\":\"fake-connection-0003\"}}\r\n";
        await File.WriteAllTextAsync(_linux.LocalPath(AppSettings), changed);

        // Act
        RegistrySyncReport push = await _linux.Sync();
        RegistrySyncReport pull = await _main.Sync();

        // Assert
        Assert.Equal((ERegistrySyncAction.Push, ERegistrySyncChange.Update), ActionOf(push, AppSettings));
        Assert.Equal((ERegistrySyncAction.Pull, ERegistrySyncChange.Update), ActionOf(pull, AppSettings));
        byte[] expected = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(changed)];
        Assert.Equal(expected, await File.ReadAllBytesAsync(_main.LocalPath(AppSettings)));
        AssertInSync(await _linux.CreatePlan());
    }

    //a file that the registry still points to is taken again and never deleted on the server
    [Fact]
    public async Task MissingFile_WhenTheRegistryStillPointsToIt_IsTakenAgain()
    {
        // Arrange
        await _main.Sync();
        await _linux.Sync();
        File.Delete(_linux.LocalPath(AppSettings));

        // Act
        RegistrySyncReport report = await _linux.Sync();

        // Assert
        Assert.Equal((ERegistrySyncAction.Pull, ERegistrySyncChange.Add), ActionOf(report, AppSettings));
        Assert.Equal(AppSettingsContent, await File.ReadAllTextAsync(_linux.LocalPath(AppSettings)));
        Assert.Equal(AppSettingsContent, _context.Server.FileContent(AppSettings));
        AssertInSync(await _linux.CreatePlan());
    }

    //file-based deletion (C6): the file stays while it exists; after it is deleted and no registry record points to it,
    //each computer deletes it only by the user's choice
    [Fact]
    public async Task Delete_WhenTheFileIsGoneAndNothingPointsToIt_HappensOnlyByTheUsersChoice()
    {
        // Arrange
        await _main.Sync();
        await _linux.Sync();
        _main.Parameters.Projects["AppFake"].ServerInfos.Clear();

        // Act
        RegistrySyncReport stillThere = await _main.Sync();
        File.Delete(_main.LocalPath(AppSettings));
        RegistrySyncReport notConfirmedOnMain = await _main.Sync();
        RegistrySyncReport confirmedOnMain = await _main.Resolve(AppSettings, ERegistryConflictResolution.Local);
        RegistrySyncReport notConfirmedOnLinux = await _linux.Sync();
        bool keptOnLinux = File.Exists(_linux.LocalPath(AppSettings));
        RegistrySyncReport confirmedOnLinux = await _linux.Resolve(AppSettings, ERegistryConflictResolution.Server);

        // Assert
        Assert.Equal(ERegistrySyncAction.InSync, ActionOf(stillThere, AppSettings).Action);
        Assert.Equal(ERegistrySyncOutcome.NotSelected, OutcomeOf(notConfirmedOnMain, AppSettings));
        Assert.Equal(ERegistrySyncOutcome.Done, OutcomeOf(confirmedOnMain, AppSettings));
        Assert.Null(_context.Server.FileContent(AppSettings));
        Assert.Equal(ERegistrySyncOutcome.NotSelected, OutcomeOf(notConfirmedOnLinux, AppSettings));
        Assert.Empty(_linux.Parameters.Projects["AppFake"].ServerInfos);
        Assert.True(keptOnLinux);
        Assert.Equal(ERegistrySyncOutcome.Done, OutcomeOf(confirmedOnLinux, AppSettings));
        Assert.False(File.Exists(_linux.LocalPath(AppSettings)));
        AssertInSync(await _main.CreatePlan());
        AssertInSync(await _linux.CreatePlan());
    }

    private static void AssertInSync(RegistrySyncPlan plan)
    {
        Assert.All(plan.Items, x =>
        {
            Assert.Equal(ERegistrySyncAction.InSync, x.Action);
            Assert.Equal(x.Local?.Hash, x.Server?.Hash);
        });
    }

    private static (ERegistrySyncAction Action, ERegistrySyncChange Change) ActionOf(RegistrySyncReport report,
        string key)
    {
        RegistrySyncPlanItem item = ItemOf(report, key).PlanItem;
        return (item.Action, item.Change);
    }

    private static ERegistrySyncOutcome OutcomeOf(RegistrySyncReport report, string key)
    {
        return ItemOf(report, key).Outcome;
    }

    private static RegistrySyncReportItem ItemOf(RegistrySyncReport report, string key)
    {
        return report.Items.Single(x =>
            x.PlanItem.CollectionName == RegistryCollections.StoredFiles &&
            string.Equals(x.PlanItem.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    //the template folders of a computer, so that the template adapters have nothing to warn about
    private static SupportToolsParameters NewParameters(string folder)
    {
        return new SupportToolsParameters
        {
            FolderForGitignoreFiles = Path.Combine(folder, "gitignore"),
            FolderForEditorConfigFiles = Path.Combine(folder, "editorconfig")
        };
    }

    private static void WriteFile(string path, string content, bool withBom)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(withBom));
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

        public string LocalPath(string canonicalPath)
        {
            return PathMapper.ToLocal(canonicalPath)!;
        }

        public async Task<RegistrySyncPlan> CreatePlan()
        {
            Result<RegistrySyncPlan> plan = await _engine.CreatePlan();
            Assert.True(plan.IsSuccess, plan.IsFailure ? plan.Error.Description : null);
            return plan.Value;
        }

        public async Task<RegistrySyncReport> Sync()
        {
            return await Execute(await CreatePlan(), RegistrySyncSelection.AllNonConflicting);
        }

        //the user's choice for the delete conflict of a stored file
        public async Task<RegistrySyncReport> Resolve(string key, ERegistryConflictResolution resolution)
        {
            RegistrySyncPlan plan = await CreatePlan();
            RegistrySyncPlanItem item = plan.Items.Single(x =>
                x.CollectionName == RegistryCollections.StoredFiles &&
                string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(ERegistrySyncConflict.DeleteNeedsConfirmation, item.Conflict);
            return await Execute(plan,
                new RegistrySyncSelection
                {
                    ConflictResolutions =
                        new Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution> { [item] = resolution }
                });
        }

        private async Task<RegistrySyncReport> Execute(RegistrySyncPlan plan, RegistrySyncSelection selection)
        {
            RegistrySyncReport report = await _engine.Execute(plan, selection);
            Assert.Null(report.TransportError);
            Assert.DoesNotContain(report.Items,
                x => x.Outcome is ERegistrySyncOutcome.Failed or ERegistrySyncOutcome.Conflict);
            return report;
        }
    }
}
