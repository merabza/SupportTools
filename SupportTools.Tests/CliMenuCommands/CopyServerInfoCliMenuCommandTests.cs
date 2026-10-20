using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class CopyServerInfoCliMenuCommandTests : IDisposable
{
    private const string MenuName = "Copy Server...";

    //the copied record is PAZISI|Production of ProjectTestData, which sets every field of it
    private const string ProjectName = ProjectTestData.Name;
    private const string ServerInfoKey = ProjectTestData.ProdKey;
    private const string ServerName = "PAZISI";
    private const string EnvironmentName = "Production";

    //another project, with Merinson|Production only
    private const string OtherProjectName = "AnotherFake";

    private readonly Queue<Func<string?, string?>> _answers = new();
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string FieldName, string? DefaultValue)> _prompts = [];
    private List<string>? _offeredProjectNames;

    public CopyServerInfoCliMenuCommandTests()
    {
        _parameters = new SupportToolsParameters
        {
            Projects =
            {
                [ProjectName] = ProjectTestData.NewProject(),
                [OtherProjectName] = new ProjectModel
                {
                    ServerInfos =
                    {
                        ["3d4c5b6a-0000-4000-8000-000000000001"] =
                            new ServerInfoModel { ServerName = "Merinson", EnvironmentName = EnvironmentName }
                    }
                }
            }
        };

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        SetupSaveResult(true);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        CopyServerInfoCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal(MenuName, sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.False(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public void PublicConstructor_WhenCreated_SetsMenuName()
    {
        // Act
        var sut = new CopyServerInfoCliMenuCommand(new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object, ProjectName, ServerInfoKey);

        // Assert
        Assert.Equal(MenuName, sut.Name);
    }

    [Fact]
    public async Task RunBody_WhenRun_AsksProjectServerAndEnvironmentWithCurrentValuesAsDefaults()
    {
        // Arrange
        (string, string?)[] expected =
        [
            ("Project", ProjectName), ("Server Name", ServerName), ("Environment Name", EnvironmentName)
        ];
        AcceptDefaults();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, _prompts);
    }

    [Fact]
    public async Task RunBody_WhenRun_OffersEveryProjectInNameOrderIgnoringCase()
    {
        // Arrange
        _parameters.Projects.Add("Zeta", new ProjectModel());
        _parameters.Projects.Add("alpha", new ProjectModel());
        string[] expected = ["alpha", OtherProjectName, ProjectName, "Zeta"];
        AcceptDefaults();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, _offeredProjectNames);
    }

    //the defaults name the copied record itself; Merinson|Test is the other record of its project
    [Theory]
    [InlineData(ProjectName, ServerName, EnvironmentName)]
    [InlineData(ProjectName, "Merinson", "Test")]
    [InlineData(ProjectName, "pazisi", "PRODUCTION")]
    [InlineData(OtherProjectName, "Merinson", EnvironmentName)]
    [InlineData(OtherProjectName, "MERINSON", "production")]
    public async Task RunBody_WhenSelectedProjectAlreadyHasServerAndEnvironment_ReportsItAndCopiesNothing(
        string projectName, string serverName, string environmentName)
    {
        // Arrange
        string parametersBefore = JsonConvert.SerializeObject(_parameters);
        AnswerWith(projectName, serverName, environmentName);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains(
            $"Server {serverName} with environment {environmentName} already exists in project {projectName}, nothing is copied",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Equal(parametersBefore, JsonConvert.SerializeObject(_parameters));
        VerifyNoSave();
    }

    //it is enough that one of the two names differs
    [Theory]
    [InlineData(ProjectName, "Merinson", EnvironmentName)]
    [InlineData(ProjectName, ServerName, "Test")]
    [InlineData(OtherProjectName, ServerName, EnvironmentName)]
    [InlineData(OtherProjectName, "Merinson", "Test")]
    public async Task RunBody_WhenServerAndEnvironmentAreNewInSelectedProject_AddsCopyWithSelectedNames(
        string projectName, string serverName, string environmentName)
    {
        // Arrange
        int countBefore = _parameters.Projects[projectName].ServerInfos.Count;

        // Act
        (_, ServerInfoModel copy) = await CopyTo(projectName, serverName, environmentName);

        // Assert
        Assert.Equal(countBefore + 1, _parameters.Projects[projectName].ServerInfos.Count);
        Assert.Equal(serverName, copy.ServerName);
        Assert.Equal(environmentName, copy.EnvironmentName);
    }

    [Fact]
    public async Task RunBody_WhenCopied_GivesCopyNewGuidKey()
    {
        // Act
        (string key, _) = await CopyTo(ProjectName, ServerName, "Test");

        // Assert
        Assert.True(Guid.TryParse(key, out _));
    }

    [Fact]
    public async Task RunBody_WhenCopied_CopiesEveryParameterAndSubParameter()
    {
        // Arrange
        ServerInfoModel source = _parameters.Projects[ProjectName].ServerInfos[ServerInfoKey];
        JObject expected = JObject.FromObject(source);
        expected[nameof(ServerInfoModel.ServerName)] = "Merinson";
        expected[nameof(ServerInfoModel.EnvironmentName)] = "Test";

        // Act
        (_, ServerInfoModel copy) = await CopyTo(OtherProjectName, "Merinson", "Test");

        // Assert
        //the premise: every field of the source is set, the database parameters included
        Assert.DoesNotContain(expected.Descendants(), x => x.Type == JTokenType.Null);
        Assert.Equal(expected.ToString(), JObject.FromObject(copy).ToString());
    }

    [Fact]
    public async Task RunBody_WhenCopied_CopyDoesNotShareObjectsWithSource()
    {
        // Arrange
        ServerInfoModel source = _parameters.Projects[ProjectName].ServerInfos[ServerInfoKey];

        // Act
        (_, ServerInfoModel copy) = await CopyTo(ProjectName, ServerName, "Test");

        // Assert
        Assert.NotSame(source.AllowToolsList, copy.AllowToolsList);
        Assert.NotSame(source.CurrentDatabaseParameters, copy.CurrentDatabaseParameters);
        Assert.NotSame(source.NewDatabaseParameters, copy.NewDatabaseParameters);
    }

    [Theory]
    [InlineData(ProjectName)]
    [InlineData(OtherProjectName)]
    public async Task RunBody_WhenCopied_KeepsSourceRecordUnchanged(string projectName)
    {
        // Arrange
        string sourceBefore = JsonConvert.SerializeObject(_parameters.Projects[ProjectName].ServerInfos[ServerInfoKey]);

        // Act
        await CopyTo(projectName, "Merinson", "Test2");

        // Assert
        Assert.Equal(sourceBefore,
            JsonConvert.SerializeObject(_parameters.Projects[ProjectName].ServerInfos[ServerInfoKey]));
    }

    [Fact]
    public async Task RunBody_WhenCopiedToOtherProject_LeavesCurrentProjectUnchanged()
    {
        // Arrange
        string projectBefore = JsonConvert.SerializeObject(_parameters.Projects[ProjectName]);

        // Act
        await CopyTo(OtherProjectName, ServerName, EnvironmentName);

        // Assert
        Assert.Equal(projectBefore, JsonConvert.SerializeObject(_parameters.Projects[ProjectName]));
    }

    [Fact]
    public async Task RunBody_WhenCopied_SavesRootParametersOnceWithMessage()
    {
        // Act
        await CopyTo(OtherProjectName, ServerName, EnvironmentName);

        // Assert
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        _parametersManager.Verify(
            x => x.Save(_parameters, $"Server {ServerName}|{EnvironmentName} copied to project {OtherProjectName}",
                null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunBody_WhenSaveFails_ReturnsFalse()
    {
        // Arrange
        SetupSaveResult(false);
        AnswerWith(OtherProjectName, ServerName, EnvironmentName);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(ProjectName, "MissingKey")]
    [InlineData("MissingProject", ServerInfoKey)]
    public async Task RunBody_WhenServerDoesNotExist_ReturnsFalseWithoutAsking(string projectName,
        string serverInfoKey)
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut(projectName, serverInfoKey));

        // Assert
        Assert.False(result);
        Assert.Empty(_prompts);
        Assert.Contains($"Server with key {serverInfoKey} does not exist in project {projectName}", ConsoleText(),
            StringComparison.Ordinal);
        VerifyNoSave();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("MissingProject")]
    public async Task RunBody_WhenSelectedProjectDoesNotExist_ReturnsFalseWithoutAskingNames(string? projectName)
    {
        // Arrange
        string parametersBefore = JsonConvert.SerializeObject(_parameters);
        _answers.Enqueue(_ => projectName);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Single(_prompts);
        Assert.Contains($"Project {projectName} does not exist", ConsoleText(), StringComparison.Ordinal);
        Assert.Equal(parametersBefore, JsonConvert.SerializeObject(_parameters));
        VerifyNoSave();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RunBody_WhenSelectionIsEscaped_PropagatesEscapeAndCopiesNothing(int escapedQuestionIndex)
    {
        // Arrange
        string parametersBefore = JsonConvert.SerializeObject(_parameters);
        for (int i = 0; i < escapedQuestionIndex; i++)
        {
            _answers.Enqueue(defaultValue => defaultValue);
        }

        _answers.Enqueue(_ => throw new DataInputEscapeException("Escape"));

        // Act
        await Assert.ThrowsAsync<DataInputEscapeException>(() => CliMenuTestAccess.InvokeRunBody(CreateSut()));

        // Assert
        Assert.Equal(parametersBefore, JsonConvert.SerializeObject(_parameters));
        VerifyNoSave();
    }

    private CopyServerInfoCliMenuCommand CreateSut(string projectName = ProjectName,
        string serverInfoKey = ServerInfoKey)
    {
        return new CopyServerInfoCliMenuCommand(_parametersManager.Object, projectName, serverInfoKey,
            (fieldName, projectNames, currentProjectName) =>
            {
                _offeredProjectNames = projectNames;
                return Answer(fieldName, currentProjectName);
            }, (fieldName, currentServerName, _) => ValueTask.FromResult(Answer(fieldName, currentServerName)),
            (fieldName, currentEnvironmentName, _) => ValueTask.FromResult(Answer(fieldName, currentEnvironmentName)));
    }

    //an unexpected question finds the queue empty, and Dequeue fails the test
    private string? Answer(string fieldName, string? defaultValue)
    {
        _prompts.Add((fieldName, defaultValue));
        return _answers.Dequeue()(defaultValue);
    }

    private void AcceptDefaults()
    {
        for (int i = 0; i < 3; i++)
        {
            _answers.Enqueue(defaultValue => defaultValue);
        }
    }

    private void AnswerWith(string projectName, string serverName, string environmentName)
    {
        _answers.Enqueue(_ => projectName);
        _answers.Enqueue(_ => serverName);
        _answers.Enqueue(_ => environmentName);
    }

    //runs a copy that must succeed and returns the record it added
    private async Task<(string Key, ServerInfoModel Copy)> CopyTo(string projectName, string serverName,
        string environmentName)
    {
        List<string> keysBefore = [.. _parameters.Projects[projectName].ServerInfos.Keys];
        AnswerWith(projectName, serverName, environmentName);

        Assert.True(await CliMenuTestAccess.InvokeRunBody(CreateSut()));

        Dictionary<string, ServerInfoModel> serverInfos = _parameters.Projects[projectName].ServerInfos;
        string key = Assert.Single(serverInfos.Keys.Except(keysBefore));
        return (key, serverInfos[key]);
    }

    private void SetupSaveResult(bool result)
    {
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(result);
    }

    private void VerifyNoSave()
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }
}
