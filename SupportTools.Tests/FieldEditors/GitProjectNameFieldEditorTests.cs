using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibGitData.Models;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.FieldEditors;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitProjectNameFieldEditorTests : IDisposable
{
    private const string CsProjExtension = ".csproj";
    private const string EsProjExtension = ".esproj";

    //the answers of the selection: Enter keeps the current value, "-" is the key of the (None) item, any other answer
    //is the name of an offered project
    private const string EnterAnswer = "<Enter>";
    private const string NoneAnswer = "-";
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

    //the project files of the project's git, of another git and of no git
    private readonly SupportToolsParameters _parameters = new()
    {
        GitProjects =
        {
            ["WebAgentContracts"] =
                new GitProjectDataModel { GitName = "WebAgent", ProjectFileName = "WebAgentContracts.csproj" },
            ["WebAgent"] =
                new GitProjectDataModel { GitName = "WebAgent", ProjectFileName = "WebAgent.csproj" },
            ["WebAgentFront"] =
                new GitProjectDataModel { GitName = "WebAgent", ProjectFileName = "WebAgentFront.esproj" },
            ["LibWebAgentRepositories"] =
                new GitProjectDataModel { GitName = "WebAgent", ProjectFileName = "LibWebAgentRepositories.csproj" },
            ["OtherApp"] =
                new GitProjectDataModel { GitName = "OtherApp", ProjectFileName = "OtherApp.csproj" },
            ["NoGitApp"] = new GitProjectDataModel { ProjectFileName = "NoGitApp.csproj" }
        }
    };

    private readonly Mock<IParametersManager> _parametersManager = new();

    private readonly ProjectModel _project = new()
    {
        GitProjectNames = ["WebAgent"],
        MainProjectName = "WebAgent",
        ApiContractsProjectName = "WebAgentContracts",
        SpaProjectName = "WebAgentFront"
    };

    private readonly List<(string FieldName, CliMenuSet MenuSet, string? CurrentName)> _selections = [];
    private string _answer = EnterAnswer;

    public GitProjectNameFieldEditorTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    //the public constructor selects on the console: the test host has no real console, so the selection throws after
    //the prompt with the current name is printed, and nothing is changed
    [Fact]
    public async Task PublicConstructor_WhenUpdatingField_PromptsWithCurrentProjectName()
    {
        // Arrange
        var sut = new GitProjectNameFieldEditor(nameof(ProjectModel.MainProjectName), GitProjectNamesParameterNames(),
            CsProjExtension, _parametersManager.Object, true);

        // Act
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await sut.UpdateField(null, _project, CancellationToken.None));

        // Assert
        Assert.Contains("Select Main Project Name [WebAgent]: ", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal("WebAgent", _project.MainProjectName);
    }

    //only the projects of the record's gits with the editor's extension, in name order
    [Theory]
    [InlineData(nameof(ProjectModel.ApiContractsProjectName), CsProjExtension, "Api Contracts Project Name",
        "WebAgentContracts", new[] { "LibWebAgentRepositories", "WebAgent", "WebAgentContracts" })]
    [InlineData(nameof(ProjectModel.SpaProjectName), EsProjExtension, "Spa Project Name", "WebAgentFront",
        new[] { "WebAgentFront" })]
    public async Task UpdateField_WhenSelecting_OffersProjectsOfRecordGitsWithExtensionInOrder(string propertyName,
        string projectExtension, string expectedFieldName, string expectedCurrentName, string[] expectedNames)
    {
        // Act
        await CreateSut(propertyName, projectExtension, false).UpdateField(null, _project, CancellationToken.None);

        // Assert
        (string fieldName, CliMenuSet menuSet, string? currentName) = Assert.Single(_selections);
        Assert.Equal(expectedFieldName, fieldName);
        Assert.Equal(expectedCurrentName, currentName);
        Assert.Equal(expectedNames, CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.MenuItemName));
    }

    //(None) is offered only by the editors created with useNone, as the first item with the key "-"
    [Theory]
    [InlineData(true, new[] { "(None)", "LibWebAgentRepositories", "WebAgent", "WebAgentContracts" })]
    [InlineData(false, new[] { "LibWebAgentRepositories", "WebAgent", "WebAgentContracts" })]
    public async Task UpdateField_WhenSelecting_OffersNoneOnlyWithUseNone(bool useNone, string[] expectedNames)
    {
        // Act
        await CreateSut(nameof(ProjectModel.MainProjectName), CsProjExtension, useNone)
            .UpdateField(null, _project, CancellationToken.None);

        // Assert
        List<CliMenuItem> items = CliMenuTestAccess.GetMenuItems(Assert.Single(_selections).MenuSet);
        Assert.Equal(expectedNames, items.Select(x => x.MenuItemName));
        Assert.Equal(useNone ? NoneAnswer : null, items[0].Key);
    }

    //Enter used to clear the value (useNone) or to throw "Selected invalid ID" (without useNone): the items had no id,
    //so the list returned -1 for the current one
    [Theory]
    [InlineData(nameof(ProjectModel.MainProjectName), CsProjExtension, true, "WebAgent")]
    [InlineData(nameof(ProjectModel.ApiContractsProjectName), CsProjExtension, false, "WebAgentContracts")]
    [InlineData(nameof(ProjectModel.SpaProjectName), EsProjExtension, false, "WebAgentFront")]
    public async Task UpdateField_WhenEnterPressed_KeepsCurrentProjectName(string propertyName, string projectExtension,
        bool useNone, string currentName)
    {
        // Arrange
        _answer = EnterAnswer;

        // Act
        await CreateSut(propertyName, projectExtension, useNone).UpdateField(null, _project, CancellationToken.None);

        // Assert
        Assert.Equal(currentName, GetProjectName(propertyName));
    }

    //the (None) item has a key, so it does not move the ids of the projects
    [Theory]
    [InlineData(true, "LibWebAgentRepositories")]
    [InlineData(true, "WebAgentContracts")]
    [InlineData(false, "LibWebAgentRepositories")]
    [InlineData(false, "WebAgentContracts")]
    public async Task UpdateField_WhenOtherProjectSelected_StoresIt(bool useNone, string projectName)
    {
        // Arrange
        _answer = projectName;

        // Act
        await CreateSut(nameof(ProjectModel.MainProjectName), CsProjExtension, useNone)
            .UpdateField(null, _project, CancellationToken.None);

        // Assert
        Assert.Equal(projectName, _project.MainProjectName);
    }

    [Fact]
    public async Task UpdateField_WhenNoneSelected_ClearsProjectName()
    {
        // Arrange
        _answer = NoneAnswer;

        // Act
        await CreateSut(nameof(ProjectModel.MainProjectName), CsProjExtension, true)
            .UpdateField(null, _project, CancellationToken.None);

        // Assert
        Assert.Null(_project.MainProjectName);
    }

    //a project whose gits are not set yet has nothing to offer: nothing is asked and the value stays
    [Fact]
    public async Task UpdateField_WhenNoProjectOfRecordGits_ThrowsListIsEmptyException()
    {
        // Arrange
        var project = new ProjectModel { MainProjectName = "WebAgent" };

        // Act
        await Assert.ThrowsAsync<ListIsEmptyException>(async () =>
            await CreateSut(nameof(ProjectModel.MainProjectName), CsProjExtension, true)
                .UpdateField(null, project, CancellationToken.None));

        // Assert
        Assert.Empty(_selections);
        Assert.Equal("WebAgent", project.MainProjectName);
    }

    private GitProjectNameFieldEditor CreateSut(string propertyName, string projectExtension, bool useNone)
    {
        return new GitProjectNameFieldEditor(propertyName, GitProjectNamesParameterNames(), projectExtension,
            _parametersManager.Object, useNone, InputIdFromMenuList);
    }

    //as ProjectCruder creates the editors
    private static string[] GitProjectNamesParameterNames()
    {
        return [nameof(ProjectModel.GitProjectNames)];
    }

    private string? GetProjectName(string propertyName)
    {
        return (string?)typeof(ProjectModel).GetProperty(propertyName)!.GetValue(_project);
    }

    //the ids as SelectFromMenuListInput returns them: Enter gives the CountedId of the item with the current name, the
    //"-" key gives -1, and the key of an item without a key of its own gives the item's position among those items
    private int InputIdFromMenuList(string fieldName, CliMenuSet menuSet, string? currentName)
    {
        _selections.Add((fieldName, menuSet, currentName));
        List<CliMenuItem> items = CliMenuTestAccess.GetMenuItems(menuSet);
        switch (_answer)
        {
            case EnterAnswer:
                CliMenuItem? currentItem = menuSet.GetMenuItemWithName(currentName!);
                Assert.NotNull(currentItem);
                return currentItem.CountedId;
            case NoneAnswer:
                Assert.Contains(items, x => x.Key == NoneAnswer);
                return -1;
            default:
                int position = items.Where(x => x.Key is null).ToList().FindIndex(x => x.MenuItemName == _answer);
                Assert.True(position >= 0, $"{_answer} is not offered for {fieldName}");
                return position;
        }
    }
}
