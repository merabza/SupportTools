using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SelectProjectAllowToolsCliMenuCommandTests : IDisposable
{
    private const string ProjectName = "MyProject";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public SelectProjectAllowToolsCliMenuCommandTests()
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

    [Fact]
    public void Constructor_WhenCreated_NamesCommandAndReloadsMenu()
    {
        // Act
        var sut = new SelectProjectAllowToolsCliMenuCommand(_parametersManager.Object, ProjectName);

        // Assert
        Assert.Equal("Select Allow tools...", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WhenProjectIsMissing_ReturnsFalseWithoutSelection()
    {
        // Arrange
        bool selected = false;
        var sut = CreateSut((_, _) => selected = true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.False(result);
        Assert.False(selected);
        Assert.Contains($"Project with name {ProjectName} does not exists", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenProjectUsesDatabase_OffersEveryToolByVisibleNameWithCurrentChoices()
    {
        // Arrange
        AddProject(new ProjectModel { AllowToolsList = [EProjectTools.GenerateApiRoutes] });
        string? caption = null;
        Dictionary<string, bool>? offered = null;
        var sut = CreateSut((c, checks) =>
        {
            caption = c;
            offered = new Dictionary<string, bool>(checks);
        });

        // Act
        await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.Equal("Select allow tools", caption);
        Assert.Equal(Enum.GetValues<EProjectTools>().Select(x => x.GetProjectToolName()), offered!.Keys);
        Assert.True(offered[EProjectTools.GenerateApiRoutes.GetProjectToolName()]);
        Assert.False(offered[EProjectTools.DropDevDatabase.GetProjectToolName()]);
    }

    [Fact]
    public async Task RunBody_WhenProjectIsDbTools_OffersDatabaseToolsOnly()
    {
        // Arrange
        AddProject(new ProjectModel { ProjectType = EProjectType.IsDbTools });
        Dictionary<string, bool>? offered = null;
        var sut = CreateSut((_, checks) => offered = new Dictionary<string, bool>(checks));

        // Act
        await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.Equal(
            Enum.GetValues<EProjectTools>().Where(x => x.IsDatabaseTool()).Select(x => x.GetProjectToolName()),
            offered!.Keys);
    }

    [Fact]
    public async Task RunBody_WhenSelectionChanges_AddsAndRemovesToolsAndSaves()
    {
        // Arrange
        ProjectModel project = AddProject(new ProjectModel
        {
            AllowToolsList = [EProjectTools.GenerateApiRoutes, EProjectTools.DropDevDatabase]
        });
        var sut = CreateSut((_, checks) =>
        {
            checks[EProjectTools.DropDevDatabase.GetProjectToolName()] = false;
            checks[EProjectTools.JetBrainsCleanupCode.GetProjectToolName()] = true;
            checks[EProjectTools.GenerateApiRoutes.GetProjectToolName()] = true;
        });

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.True(result);
        Assert.Equal([EProjectTools.GenerateApiRoutes, EProjectTools.JetBrainsCleanupCode], project.AllowToolsList);
        _parametersManager.Verify(x => x.Save(_parameters, "Changes saved", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    //the choices of the hidden tools stay as they were
    [Fact]
    public async Task RunBody_WhenProjectDoesNotUseDatabase_KeepsHiddenDatabaseToolChoice()
    {
        // Arrange
        ProjectModel project = AddProject(new ProjectModel
        {
            UsesDatabase = false, AllowToolsList = [EProjectTools.RecreateDevDatabase, EProjectTools.GenerateApiRoutes]
        });
        Dictionary<string, bool>? offered = null;
        var sut = CreateSut((_, checks) =>
        {
            offered = new Dictionary<string, bool>(checks);
            checks[EProjectTools.GenerateApiRoutes.GetProjectToolName()] = false;
        });

        // Act
        await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.Equal(
            [EProjectTools.JetBrainsCleanupCode.GetProjectToolName(), EProjectTools.GenerateApiRoutes.GetProjectToolName()],
            offered!.Keys);
        Assert.Equal([EProjectTools.RecreateDevDatabase], project.AllowToolsList);
    }

    [Fact]
    public async Task RunBody_WhenSelectionHasUnknownTool_ReturnsFalseWithoutSaving()
    {
        // Arrange
        AddProject(new ProjectModel());
        var sut = CreateSut((_, checks) => checks["No Such Tool"] = true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.False(result);
        VerifyNotSaved();
    }

    //the public constructor selects on the console, which the test host does not have
    [Fact]
    public async Task RunBody_WhenCreatedWithPublicConstructor_SelectsOnConsole()
    {
        // Arrange
        AddProject(new ProjectModel());
        var sut = new SelectProjectAllowToolsCliMenuCommand(_parametersManager.Object, ProjectName);

        // Act / Assert
        await Assert.ThrowsAnyAsync<Exception>(() => CliMenuTestAccess.InvokeRunBody(sut));
        VerifyNotSaved();
    }

    private SelectProjectAllowToolsCliMenuCommand CreateSut(Action<string, Dictionary<string, bool>> selectTools)
    {
        return new SelectProjectAllowToolsCliMenuCommand(_parametersManager.Object, ProjectName, selectTools);
    }

    private ProjectModel AddProject(ProjectModel project)
    {
        _parameters.Projects[ProjectName] = project;
        return project;
    }

    private void VerifyNotSaved()
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
}
