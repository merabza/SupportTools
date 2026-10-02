using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using LibGitData;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using ToolsManagement.ApiClientsManagement;
using Xunit;

namespace SupportTools.Tests.Models;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SupportToolsParametersTests : IDisposable
{
    private const string ProjectName = "FakeProject";
    private const string GitName = "FakeGit";
    private const string SeederGitName = "FakeSeederGit";
    private const EGitCol UnknownGitCol = (EGitCol)99;

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly ProjectModel _project;
    private readonly SupportToolsParameters _sut;

    public SupportToolsParametersTests()
    {
        _project = new ProjectModel
        {
            ProjectFolderName = @"D:\Work\FakeProject",
            ScaffoldSeederProjectName = "FakeSeeder",
            GitProjectNames = [GitName, "OtherGit"],
            ScaffoldSeederGitProjectNames = [SeederGitName],
            FrontNpmPackageNames = ["react"],
            ServerInfos = { ["Server|Prod"] = new ServerInfoModel { ServerName = "Server" } }
        };
        _sut = new SupportToolsParameters { Projects = { [ProjectName] = _project } };

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void CheckBeforeSave_WhenCalled_AllowsSaving()
    {
        // Act
        bool result = _sut.CheckBeforeSave();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSupportToolsServerApiClient_WhenApiClientNameIsNotSet_ReportsItAndReturnsNull(string? apiClientName)
    {
        // Arrange
        _sut.SupportToolsServerWebApiClientName = apiClientName;

        // Act
        SupportToolsServerApiClient? result =
            _sut.GetSupportToolsServerApiClient(null, new Mock<IHttpClientFactory>().Object);

        // Assert
        Assert.Null(result);
        Assert.Contains("supportToolsServerWebApiClientName does not specified", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void GetSupportToolsServerApiClient_WhenApiClientDoesNotExist_Throws()
    {
        // Arrange
        _sut.SupportToolsServerWebApiClientName = "MissingClient";

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.GetSupportToolsServerApiClient(null, new Mock<IHttpClientFactory>().Object));

        // Assert
        Assert.Equal("ApiClient with name MissingClient does not exists", exception.Message);
    }

    //the address is never called here: an unreachable local address keeps the test offline
    [Fact]
    public void GetSupportToolsServerApiClient_WhenApiClientExists_CreatesClient()
    {
        // Arrange
        _sut.SupportToolsServerWebApiClientName = "ServerClient";
        _sut.ApiClients["ServerClient"] = new ApiClientSettings { Server = "http://127.0.0.1:0/", ApiKey = "fake" };

        // Act
        SupportToolsServerApiClient? result = _sut.GetSupportToolsServerApiClient(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(ConsoleText());
    }

    [Fact]
    public void GetUploadTempExtensionOrDefault_WhenExtensionIsNotSet_ReturnsDefaultExtension()
    {
        // Act
        string result = _sut.GetUploadTempExtensionOrDefault();

        // Assert
        Assert.Equal(".up!", result);
    }

    [Fact]
    public void GetUploadTempExtensionOrDefault_WhenExtensionIsSet_ReturnsIt()
    {
        // Arrange
        _sut.UploadTempExtension = ".tmp";

        // Act
        string result = _sut.GetUploadTempExtensionOrDefault();

        // Assert
        Assert.Equal(".tmp", result);
    }

    [Fact]
    public void DeleteGitFromProjectByNames_WhenProjectDoesNotExist_ReturnsFalse()
    {
        // Act
        bool result = _sut.DeleteGitFromProjectByNames("MissingProject", GitName, EGitCol.Main);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DeleteGitFromProjectByNames_WhenMainGit_RemovesItFromProjectGits()
    {
        // Arrange
        string[] expected = ["OtherGit"];

        // Act
        bool result = _sut.DeleteGitFromProjectByNames(ProjectName, GitName, EGitCol.Main);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, _project.GitProjectNames);
        Assert.Single(_project.ScaffoldSeederGitProjectNames);
    }

    [Fact]
    public void DeleteGitFromProjectByNames_WhenScaffoldSeederGit_RemovesItFromSeederGits()
    {
        // Act
        bool result = _sut.DeleteGitFromProjectByNames(ProjectName, SeederGitName, EGitCol.ScaffoldSeed);

        // Assert
        Assert.True(result);
        Assert.Empty(_project.ScaffoldSeederGitProjectNames);
        Assert.Equal(2, _project.GitProjectNames.Count);
    }

    [Fact]
    public void DeleteGitFromProjectByNames_WhenGitCollectionIsUnknown_ReturnsFalseWithoutChanges()
    {
        // Act
        bool result = _sut.DeleteGitFromProjectByNames(ProjectName, GitName, UnknownGitCol);

        // Assert
        Assert.False(result);
        Assert.Equal(2, _project.GitProjectNames.Count);
        Assert.Single(_project.ScaffoldSeederGitProjectNames);
    }

    [Fact]
    public void GetGitProjectNames_WhenProjectDoesNotExist_ReturnsEmptyList()
    {
        // Act
        List<string> result = _sut.GetGitProjectNames("MissingProject", EGitCol.Main);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetGitProjectNames_WhenMainGits_ReturnsProjectGits()
    {
        // Act
        List<string> result = _sut.GetGitProjectNames(ProjectName, EGitCol.Main);

        // Assert
        Assert.Same(_project.GitProjectNames, result);
    }

    [Fact]
    public void GetGitProjectNames_WhenScaffoldSeederGits_ReturnsSeederGits()
    {
        // Act
        List<string> result = _sut.GetGitProjectNames(ProjectName, EGitCol.ScaffoldSeed);

        // Assert
        Assert.Same(_project.ScaffoldSeederGitProjectNames, result);
    }

    [Fact]
    public void GetNpmPackageNames_WhenProjectDoesNotExist_ReturnsEmptyList()
    {
        // Act
        List<string> result = _sut.GetNpmPackageNames("MissingProject");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetNpmPackageNames_WhenProjectExists_ReturnsItsFrontPackages()
    {
        // Act
        List<string> result = _sut.GetNpmPackageNames(ProjectName);

        // Assert
        Assert.Same(_project.FrontNpmPackageNames, result);
    }

    [Fact]
    public void GetApiClientSettingsRequired_WhenApiClientDoesNotExist_Throws()
    {
        // Act
        var exception =
            Assert.Throws<InvalidOperationException>(() => _sut.GetApiClientSettingsRequired("MissingClient"));

        // Assert
        Assert.Equal("ApiClient with name MissingClient does not exists", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetApiClientSettingsRequired_WhenServerIsNotSet_Throws(string? server)
    {
        // Arrange
        _sut.ApiClients["Client"] = new ApiClientSettings { Server = server };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.GetApiClientSettingsRequired("Client"));

        // Assert
        Assert.Equal("Server does not specified for ApiClient with name Client", exception.Message);
    }

    [Fact]
    public void GetApiClientSettingsRequired_WhenApiClientIsComplete_ReturnsServerAndKey()
    {
        // Arrange
        _sut.ApiClients["Client"] = new ApiClientSettings { Server = "http://fake-server/", ApiKey = "fake-key" };

        // Act
        ApiClientSettingsDomain result = _sut.GetApiClientSettingsRequired("Client");

        // Assert
        Assert.Equal("http://fake-server/", result.Server);
        Assert.Equal("fake-key", result.ApiKey);
    }

    [Fact]
    public void GetFileStorageRequired_WhenFileStorageDoesNotExist_Throws()
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.GetFileStorageRequired("MissingStorage"));

        // Assert
        Assert.Equal("FileStorage with name MissingStorage does not exists", exception.Message);
    }

    [Fact]
    public void GetFileStorageRequired_WhenFileStorageExists_ReturnsIt()
    {
        // Arrange
        var fileStorage = new FileStorageData { FileStoragePath = @"D:\BAK" };
        _sut.FileStorages["Storage"] = fileStorage;

        // Act
        FileStorageData result = _sut.GetFileStorageRequired("Storage");

        // Assert
        Assert.Same(fileStorage, result);
    }

    [Fact]
    public void GetSmartSchemaRequired_WhenSmartSchemaDoesNotExist_Throws()
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.GetSmartSchemaRequired("MissingSchema"));

        // Assert
        Assert.Equal("SmartSchema with name MissingSchema does not exists", exception.Message);
    }

    [Fact]
    public void GetSmartSchemaRequired_WhenSmartSchemaExists_ReturnsIt()
    {
        // Arrange
        var smartSchema = new SmartSchema();
        _sut.SmartSchemas["Schema"] = smartSchema;

        // Act
        SmartSchema result = _sut.GetSmartSchemaRequired("Schema");

        // Assert
        Assert.Same(smartSchema, result);
    }

    [Fact]
    public void GetServerByProject_WhenProjectDoesNotExist_ReturnsNull()
    {
        // Act
        ServerInfoModel? result = _sut.GetServerByProject("MissingProject", "Server|Prod");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetServerByProject_WhenProjectHasNoSuchServerInfo_ReturnsNull()
    {
        // Act
        ServerInfoModel? result = _sut.GetServerByProject(ProjectName, "Server|Test");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetServerByProject_WhenProjectHasServerInfo_ReturnsIt()
    {
        // Act
        ServerInfoModel? result = _sut.GetServerByProject(ProjectName, "Server|Prod");

        // Assert
        Assert.Same(_project.ServerInfos["Server|Prod"], result);
    }

    [Fact]
    public void GetServerDataRequired_WhenServerDoesNotExist_Throws()
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.GetServerDataRequired("MissingServer"));

        // Assert
        Assert.Equal("server with name MissingServer is not found", exception.Message);
    }

    [Fact]
    public void GetServerDataRequired_WhenServerExists_ReturnsIt()
    {
        // Arrange
        var server = new ServerDataModel();
        _sut.Servers["Server"] = server;

        // Act
        ServerDataModel result = _sut.GetServerDataRequired("Server");

        // Assert
        Assert.Same(server, result);
    }

    [Fact]
    public void GetServerData_WhenServerDoesNotExist_ReturnsNull()
    {
        // Act
        ServerDataModel? result = _sut.GetServerData("MissingServer");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetProjectRequired_WhenProjectDoesNotExist_Throws()
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.GetProjectRequired("MissingProject"));

        // Assert
        Assert.Equal("project with name MissingProject is not found", exception.Message);
    }

    [Fact]
    public void GetProjectRequired_WhenProjectExists_ReturnsIt()
    {
        // Act
        ProjectModel result = _sut.GetProjectRequired(ProjectName);

        // Assert
        Assert.Same(_project, result);
    }

    [Fact]
    public void GetProject_WhenProjectDoesNotExist_ReturnsNull()
    {
        // Act
        ProjectModel? result = _sut.GetProject("MissingProject");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetGitsFolder_WhenProjectDoesNotExist_ReturnsNull()
    {
        // Act
        string? result = _sut.GetGitsFolder("MissingProject", EGitCol.Main);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetGitsFolder_WhenMainGits_ReturnsProjectFolder()
    {
        // Act
        string? result = _sut.GetGitsFolder(ProjectName, EGitCol.Main);

        // Assert
        Assert.Equal(@"D:\Work\FakeProject", result);
        Assert.Empty(ConsoleText());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetGitsFolder_WhenMainGitsAndProjectFolderIsNotSet_ReportsItAndReturnsNull(string? projectFolderName)
    {
        // Arrange
        _sut.Projects[ProjectName] = new ProjectModel { ProjectFolderName = projectFolderName };

        // Act
        string? result = _sut.GetGitsFolder(ProjectName, EGitCol.Main);

        // Assert
        Assert.Null(result);
        Assert.Contains("ProjectFolderName must be specified", ConsoleText(), StringComparison.Ordinal);
    }

    //scaffold seeder gits live in {ScaffoldSeedersWorkFolder}\{ScaffoldSeederProjectName}\{ScaffoldSeederProjectName}ScaffoldSeeder
    [Fact]
    public void GetGitsFolder_WhenScaffoldSeederGits_ReturnsSeederFolder()
    {
        // Arrange
        _sut.ScaffoldSeedersWorkFolder = @"D:\Seeders";

        // Act
        string? result = _sut.GetGitsFolder(ProjectName, EGitCol.ScaffoldSeed);

        // Assert
        Assert.Equal(Path.Combine(@"D:\Seeders", "FakeSeeder", "FakeSeederScaffoldSeeder"), result);
        Assert.Empty(ConsoleText());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetGitsFolder_WhenScaffoldSeedersWorkFolderIsNotSet_ReturnsNull(string? scaffoldSeedersWorkFolder)
    {
        // Arrange
        _sut.ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;

        // Act
        string? result = _sut.GetGitsFolder(ProjectName, EGitCol.ScaffoldSeed);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetGitsFolder_WhenScaffoldSeederProjectNameIsNotSet_ReportsItAndReturnsNull()
    {
        // Arrange
        _sut.ScaffoldSeedersWorkFolder = @"D:\Seeders";
        _project.ScaffoldSeederProjectName = null;

        // Act
        string? result = _sut.GetGitsFolder(ProjectName, EGitCol.ScaffoldSeed);

        // Assert
        Assert.Null(result);
        Assert.Contains("ScaffoldSeederProjectName must be specified", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetGitsFolder_WhenGitCollectionIsUnknown_ReturnsNull()
    {
        // Act
        string? result = _sut.GetGitsFolder(ProjectName, UnknownGitCol);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FixProjectGroupName_WhenGroupNameIsNotSet_ReturnsNoGroupName(string? projectGroupName)
    {
        // Act
        string result = SupportToolsParameters.FixProjectGroupName(projectGroupName);

        // Assert
        Assert.Equal("__No Group__", result);
    }

    [Fact]
    public void FixProjectGroupName_WhenGroupNameIsSet_ReturnsIt()
    {
        // Act
        string result = SupportToolsParameters.FixProjectGroupName("Tools");

        // Assert
        Assert.Equal("Tools", result);
    }

    [Fact]
    public void GetGitIgnoreModelFilePath_WhenCalled_ReturnsTemplateFileInFolder()
    {
        // Act
        string result = SupportToolsParameters.GetGitIgnoreModelFilePath(@"D:\Templates", "CSharp");

        // Assert
        Assert.Equal(Path.Combine(@"D:\Templates", "CSharp.gitignore"), result);
    }

    [Fact]
    public void GetEditorConfigPatternFilePath_WhenCalled_ReturnsTemplateFileInFolder()
    {
        // Act
        string result = SupportToolsParameters.GetEditorConfigPatternFilePath(@"D:\Templates", "CSharp");

        // Assert
        Assert.Equal(Path.Combine(@"D:\Templates", "CSharp.editorconfig"), result);
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }
}
