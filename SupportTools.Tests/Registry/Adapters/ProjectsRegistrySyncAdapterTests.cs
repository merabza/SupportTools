using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Sync;
using ParametersManagement.LibDatabaseParameters;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = "projects";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly DatabaseServerConnectionsRegistrySyncAdapter _databaseServerConnections;
    private readonly EditorConfigPatternsRegistrySyncAdapter _editorConfigPatterns;
    private readonly GitsRegistrySyncAdapter _gits;
    private readonly NpmPackagesRegistrySyncAdapter _npmPackages;
    private ProjectsRegistrySyncAdapter _sut;

    public ProjectsRegistrySyncAdapterTests()
    {
        _context.Parameters.Projects[ProjectTestData.Name] = ProjectTestData.NewProject();
        _context.Parameters.Gits["AppFake"] = new GitDataModel();
        _context.Parameters.Gits["SystemTools"] = new GitDataModel();
        _context.Parameters.Gits["AppFakeScaffoldSeeder"] = new GitDataModel();
        _context.Parameters.NpmPackages["react"] = "React";
        _context.Parameters.EditorConfigPatterns.Add("default");
        _context.Parameters.DatabaseServerConnections["Dev"] = new DatabaseServerConnectionData();
        _gits = new GitsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.PathMapper,
            _context.Warnings);
        _npmPackages = new NpmPackagesRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
        _editorConfigPatterns = new EditorConfigPatternsRegistrySyncAdapter(_context.ApiClient, _context.Parameters,
            _context.Warnings);
        _databaseServerConnections = new DatabaseServerConnectionsRegistrySyncAdapter(_context.ApiClient,
            _context.Parameters, _context.Warnings);
        _sut = CreateSut();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Upsert_WhenProjectIsNew_SendsTheWholeAggregateAndReturnsItsVersion()
    {
        // Arrange
        object contract = _sut.GetLocalRecords()[ProjectTestData.Name];

        // Act
        Result<int> result = await _sut.Upsert(ProjectTestData.Name, contract, 0, default);

        // Assert
        Assert.Equal(1, result.Value);
        var stored = _context.Server.Get<StsProjectDataModel>(Area, ProjectTestData.Name);
        Assert.Equal(2, stored?.ServerInfos.Count);
        Assert.Equal("00000000-fake-key-guid-part", stored?.KeyGuidPart);
        Assert.Empty(_context.Warnings.Items);
    }

    //the expected version travels in the contract (B1)
    [Fact]
    public async Task Upsert_WhenVersionIsExpected_UpdatesTheProjectAndReturnsTheNewVersion()
    {
        // Arrange
        _context.Server.Store(Area, ProjectTestData.Name, ContractOf(ProjectTestData.NewProject()), 3);
        var contract = (StsProjectDataModel)_sut.GetLocalRecords()[ProjectTestData.Name];
        contract.ProjectDescription = "Changed";

        // Act
        Result<int> result = await _sut.Upsert(ProjectTestData.Name, contract, 3, default);

        // Assert
        Assert.Equal(4, result.Value);
        Assert.Equal("Changed",
            _context.Server.Get<StsProjectDataModel>(Area, ProjectTestData.Name)?.ProjectDescription);
    }

    [Fact]
    public async Task Upsert_WhenServerHasAnotherVersion_ReturnsConcurrencyConflict()
    {
        // Arrange
        _context.Server.Store(Area, ProjectTestData.Name, ContractOf(ProjectTestData.NewProject()), 5);
        object contract = _sut.GetLocalRecords()[ProjectTestData.Name];

        // Act
        Result<int> result = await _sut.Upsert(ProjectTestData.Name, contract, 3, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
        Assert.Equal(5, _context.Server.VersionOf(Area, ProjectTestData.Name));
    }

    [Fact]
    public void Normalize_WhenCalled_GivesTheNormalizedContract()
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ProjectDescription = "";
        contract.ServerInfos.Reverse();

        // Act
        var result = (StsProjectDataModel)_sut.Normalize(contract);

        // Assert
        Assert.Null(result.ProjectDescription);
        Assert.Equal(["Merinson", "PAZISI"], result.ServerInfos.Select(x => x.ServerName));
    }

    [Fact]
    public void CollectionName_WhenCreated_IsTheStateKeyAndOrderIsTheLast()
    {
        // Assert
        Assert.Equal(("Projects", 170), (_sut.CollectionName, _sut.Order));
    }

    //a repeated ServerInfo key (or any repeated value) is a local error: the project is not sent and a warning says why
    [Fact]
    public async Task Upsert_WhenValuesRepeat_ReturnsLocalRecordIsInvalidWithoutRequest()
    {
        // Arrange
        ProjectModel project = _context.Parameters.Projects[ProjectTestData.Name];
        project.ServerInfos["5e1f0000-fake-guid"] =
            new ServerInfoModel { ServerName = "pazisi", EnvironmentName = "Production" };
        project.GitProjectNames.Add("appfake");
        object contract = _sut.GetLocalRecords()[ProjectTestData.Name];

        // Act
        Result<int> result = await _sut.Upsert(ProjectTestData.Name, contract, 0, default);

        // Assert
        Assert.Equal(nameof(RegistrySyncErrors.LocalRecordIsInvalid), result.Error.Code);
        Assert.Equal(
            "Projects/AppFake: the local record is not pushed until it is fixed: GitProjectNames: AppFake/appfake; " +
            "ServerInfos: PAZISI|Production/pazisi|Production", result.Error.Description);
        Assert.Equal(0, _context.Server.RequestCount("POST", "/projects/"));
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal(ProjectTestData.Name, warning.Key);
        Assert.Equal(
            "Repeated values (GitProjectNames: AppFake/appfake; ServerInfos: PAZISI|Production/pazisi|Production), " +
            "the project is not pushed until they are fixed", warning.Message);
    }

    //an unknown tool name of a newer client would be lost by a pull and deleted on the server by a push: the project
    //stays out of the sync on both sides until SupportTools is updated
    [Fact]
    public async Task GetServerRecords_WhenServerProjectHasUnknownTool_LeavesItOutOnBothSidesWithWarning()
    {
        // Arrange
        StsProjectDataModel serverProject = ContractOf(ProjectTestData.NewProject());
        serverProject.ServerInfos[0].AllowToolsList.Add("FutureServerTool");
        _context.Server.Store(Area, ProjectTestData.Name, serverProject, 4);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await _sut.GetServerRecords(default);
        IReadOnlyDictionary<string, object> localRecords = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Empty(localRecords);
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal(ProjectTestData.Name, warning.Key);
        Assert.Contains("ServerInfos.PAZISI|Production.AllowToolsList", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetServerRecords_WhenEveryNameIsKnown_ReturnsTheProjectWithItsVersion()
    {
        // Arrange
        _context.Server.Store(Area, ProjectTestData.Name, ContractOf(ProjectTestData.NewProject()), 4);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(4, result.Value[ProjectTestData.Name].Version);
        Assert.Empty(_context.Warnings.Items);
    }

    //a reference is fine when it exists locally or on the server
    [Fact]
    public async Task GetLocalRecords_WhenReferenceIsNeitherLocalNorOnServer_WarnsAboutIt()
    {
        // Arrange
        _context.Parameters.Gits.Remove("SystemTools");
        _context.Parameters.Gits.Remove("AppFake");
        _context.Server.Store(FakeSupportToolsServer.GitRepos, "AppFake",
            new StsGitDataModel
            {
                GitProjectName = "AppFake",
                GitProjectAddress = "git@github.com:fake/AppFake.git",
                GitProjectFolderName = "AppFake",
                GitIgnorePatternName = "CSharp"
            }, 1);
        _context.Parameters.NpmPackages.Clear();
        _context.Parameters.DatabaseServerConnections.Clear();
        await _gits.GetServerRecords(default);

        // Act
        _sut.GetLocalRecords();

        // Assert
        Assert.Equal([
            "GitProjectNames SystemTools", "FrontNpmPackageNames react", "DevDatabaseParameters.DbConnectionName Dev",
            "ProdCopyDatabaseParameters.DbConnectionName Dev",
            "ServerInfos.PAZISI|Production.CurrentDatabaseParameters.DbConnectionName Dev",
            "ServerInfos.PAZISI|Production.NewDatabaseParameters.DbConnectionName Dev"
        ], _context.Warnings.Items.Select(x => x.Message.Split(" is neither")[0]));
    }

    [Fact]
    public void GetLocalRecords_WhenTemplateAndScaffoldSeederGitAreMissing_WarnsAboutThem()
    {
        // Arrange
        _context.Parameters.Gits.Remove("AppFakeScaffoldSeeder");
        _context.Parameters.EditorConfigPatterns.Clear();

        // Act
        _sut.GetLocalRecords();

        // Assert
        Assert.Equal(["ScaffoldSeederGitProjectNames AppFakeScaffoldSeeder", "EditorConfigPatternName default"],
            _context.Warnings.Items.Select(x => x.Message.Split(" is neither")[0]));
    }

    //a computer that has not pulled the referenced records yet: they are on the server, so nothing is missing
    [Fact]
    public async Task GetLocalRecords_WhenReferencesAreOnlyOnServer_DoesNotWarn()
    {
        // Arrange
        _context.Parameters.Gits.Clear();
        _context.Parameters.NpmPackages.Clear();
        _context.Parameters.EditorConfigPatterns.Clear();
        _context.Parameters.DatabaseServerConnections.Clear();
        foreach (string git in new[] { "AppFake", "SystemTools", "AppFakeScaffoldSeeder" })
        {
            _context.Server.Store(FakeSupportToolsServer.GitRepos, git,
                new StsGitDataModel
                {
                    GitProjectName = git,
                    GitProjectAddress = $"git@github.com:fake/{git}.git",
                    GitProjectFolderName = git,
                    GitIgnorePatternName = "CSharp"
                }, 1);
        }

        _context.Server.Store("npmpackages", "react", new StsNpmPackageDataModel { Name = "react" }, 1);
        _context.Server.Store(FakeSupportToolsServer.EditorConfigFileTypes, "default",
            new StsEditorConfigFileTypeDataModel { Name = "default", Content = "root = true" }, 1);
        _context.Server.Store("databaseserverconnections", "dev",
            new StsDatabaseServerConnectionDataModel { Name = "dev", DatabaseServerProvider = "SqlServer" }, 1);
        await _gits.GetServerRecords(default);
        await _npmPackages.GetServerRecords(default);
        await _editorConfigPatterns.GetServerRecords(default);
        await _databaseServerConnections.GetServerRecords(default);

        // Act
        _sut.GetLocalRecords();

        // Assert
        Assert.Empty(_context.Warnings.Items);
    }

    //a reference that exists on both sides is fine as well
    [Fact]
    public async Task GetLocalRecords_WhenReferencesAreLocalAndOnServer_DoesNotWarn()
    {
        // Arrange
        _context.Server.Store("npmpackages", "react", new StsNpmPackageDataModel { Name = "react" }, 1);
        await _npmPackages.GetServerRecords(default);

        // Act
        _sut.GetLocalRecords();

        // Assert
        Assert.Empty(_context.Warnings.Items);
    }

    //a path outside every mapping rule of a Linux computer has no canonical form
    [Fact]
    public void GetLocalRecords_WhenPathHasNoCanonicalForm_WarnsAboutIt()
    {
        // Arrange
        _context.PathMapper = MapperTestHelpers.LinuxPathMapper();
        _sut = CreateSut();
        ProjectModel project = ProjectMapper.ToLocal(ContractOf(ProjectTestData.NewProject()), null,
            _context.PathMapper);
        project.ServerInfos[ProjectTestData.TestKey].AppSettingsJsonSourceFileName = "/opt/fake/appsettings.json";
        _context.Parameters.Projects[ProjectTestData.Name] = project;

        // Act
        _sut.GetLocalRecords();

        // Assert
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Contains("/opt/fake/appsettings.json", warning.Message, StringComparison.Ordinal);
    }

    //the engine would stop the whole plan on such keys (DuplicateKeys): both projects stay out with a warning
    [Fact]
    public async Task GetRecords_WhenLocalKeysDifferOnlyByCase_LeavesThemOutOnBothSidesWithWarning()
    {
        // Arrange
        _context.Parameters.Projects["appfake"] = ProjectTestData.NewProject();
        _context.Server.Store(Area, ProjectTestData.Name, ContractOf(ProjectTestData.NewProject()), 2);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await _sut.GetServerRecords(default);
        IReadOnlyDictionary<string, object> localRecords = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Empty(localRecords);
        Assert.Contains(_context.Warnings.Items,
            x => x.Message.Contains("AppFake/appfake differ only by case", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyLocal_WhenProjectExists_UpdatesTheSameObjectAndKeepsServerInfoKeys()
    {
        // Arrange
        ProjectModel existing = _context.Parameters.Projects[ProjectTestData.Name];
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.MinorVersion = 8;
        contract.ServerInfos.RemoveAt(1);

        // Act
        _sut.ApplyLocal(ProjectTestData.Name, contract);

        // Assert
        Assert.Same(existing, _context.Parameters.Projects[ProjectTestData.Name]);
        Assert.Equal(8, existing.MinorVersion);
        Assert.Equal([ProjectTestData.ProdKey], existing.ServerInfos.Keys);
    }

    [Fact]
    public void ApplyLocal_WhenProjectIsNew_AddsIt()
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.Name = "AppOther";

        // Act
        _sut.ApplyLocal("AppOther", contract);

        // Assert
        Assert.Equal(ProjectTestData.Folder, _context.Parameters.Projects["AppOther"].ProjectFolderName);
    }

    [Fact]
    public async Task Delete_WhenVersionIsExpected_RemovesTheProjectOnServer()
    {
        // Arrange
        _context.Server.Store(Area, ProjectTestData.Name, ContractOf(ProjectTestData.NewProject()), 3);

        // Act
        Result result = await _sut.Delete(ProjectTestData.Name, 3, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Server.Records(Area));
    }

    private ProjectsRegistrySyncAdapter CreateSut()
    {
        return new ProjectsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.PathMapper,
            new ProjectReferenceServerKeys(_gits, _npmPackages, _editorConfigPatterns, _databaseServerConnections),
            _context.Warnings);
    }

    private static StsProjectDataModel ContractOf(ProjectModel project)
    {
        return ProjectMapper.ToContract(ProjectTestData.Name, project, MapperTestHelpers.WindowsPathMapper());
    }
}
