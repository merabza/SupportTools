using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SelectServerAllowToolsCliMenuCommandTests : IDisposable
{
    private const string ProjectName = "MyProject";
    private const string ServerKey = "dl360|Prod";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public SelectServerAllowToolsCliMenuCommandTests()
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
        var sut = new SelectServerAllowToolsCliMenuCommand(_parametersManager.Object, ProjectName, ServerKey);

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
        Assert.Contains($"Server with name {ServerKey} is not exists", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenServerIsMissing_ReturnsFalseWithoutSelection()
    {
        // Arrange
        _parameters.Projects[ProjectName] = new ProjectModel();
        bool selected = false;
        var sut = CreateSut((_, _) => selected = true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.False(result);
        Assert.False(selected);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenProjectUsesDatabase_OffersEveryToolWithCurrentChoices()
    {
        // Arrange
        AddServer(new ProjectModel(), [EProjectServerTools.ProgramUpdater]);
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
        Assert.Equal(Enum.GetNames<EProjectServerTools>(), offered!.Keys);
        Assert.True(offered[nameof(EProjectServerTools.ProgramUpdater)]);
        Assert.False(offered[nameof(EProjectServerTools.ServiceStarter)]);
    }

    [Fact]
    public async Task RunBody_WhenProjectIsDbTools_OffersDatabaseToolsOnly()
    {
        // Arrange
        AddServer(new ProjectModel { ProjectType = EProjectType.IsDbTools }, null);
        Dictionary<string, bool>? offered = null;
        var sut = CreateSut((_, checks) => offered = new Dictionary<string, bool>(checks));

        // Act
        await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.Equal(
            [
                nameof(EProjectServerTools.DevBaseToServerCopier),
                nameof(EProjectServerTools.ServerBaseToProdCopyCopier),
                nameof(EProjectServerTools.ServerBaseToServerCopier)
            ], offered!.Keys);
    }

    [Fact]
    public async Task RunBody_WhenSelectionChanges_AddsAndRemovesToolsAndSaves()
    {
        // Arrange
        ServerInfoModel server = AddServer(new ProjectModel(),
            [EProjectServerTools.ProgramUpdater, EProjectServerTools.ServiceStarter]);
        var sut = CreateSut((_, checks) =>
        {
            checks[nameof(EProjectServerTools.ServiceStarter)] = false;
            checks[nameof(EProjectServerTools.VersionChecker)] = true;
        });

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.True(result);
        Assert.Equal([EProjectServerTools.ProgramUpdater, EProjectServerTools.VersionChecker],
            server.AllowToolsList);
        _parametersManager.Verify(x => x.Save(_parameters, "Changes saved", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunBody_WhenServerHasNoAllowToolsList_CreatesIt()
    {
        // Arrange
        ServerInfoModel server = AddServer(new ProjectModel(), null);
        var sut = CreateSut((_, checks) => checks[nameof(EProjectServerTools.ServiceStopper)] = true);

        // Act
        await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.Equal([EProjectServerTools.ServiceStopper], server.AllowToolsList);
    }

    //the choices of the hidden tools stay as they were
    [Fact]
    public async Task RunBody_WhenProjectDoesNotUseDatabase_KeepsHiddenDatabaseToolChoice()
    {
        // Arrange
        ServerInfoModel server = AddServer(new ProjectModel { UsesDatabase = false },
            [EProjectServerTools.DevBaseToServerCopier, EProjectServerTools.ProgramUpdater]);
        Dictionary<string, bool>? offered = null;
        var sut = CreateSut((_, checks) =>
        {
            offered = new Dictionary<string, bool>(checks);
            checks[nameof(EProjectServerTools.ProgramUpdater)] = false;
        });

        // Act
        await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.DoesNotContain(nameof(EProjectServerTools.DevBaseToServerCopier), offered!.Keys);
        Assert.Equal([EProjectServerTools.DevBaseToServerCopier], server.AllowToolsList);
    }

    [Fact]
    public async Task RunBody_WhenSelectionHasUnknownTool_ReturnsFalseWithoutSaving()
    {
        // Arrange
        AddServer(new ProjectModel(), null);
        var sut = CreateSut((_, checks) => checks["NoSuchTool"] = true);

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
        AddServer(new ProjectModel(), null);
        var sut = new SelectServerAllowToolsCliMenuCommand(_parametersManager.Object, ProjectName, ServerKey);

        // Act / Assert
        await Assert.ThrowsAnyAsync<Exception>(() => CliMenuTestAccess.InvokeRunBody(sut));
        VerifyNotSaved();
    }

    private SelectServerAllowToolsCliMenuCommand CreateSut(Action<string, Dictionary<string, bool>> selectTools)
    {
        return new SelectServerAllowToolsCliMenuCommand(_parametersManager.Object, ProjectName, ServerKey,
            selectTools);
    }

    private ServerInfoModel AddServer(ProjectModel project, List<EProjectServerTools>? allowedTools)
    {
        var server = new ServerInfoModel
        {
            ServerName = "dl360", EnvironmentName = "Prod", AllowToolsList = allowedTools
        };
        project.ServerInfos[ServerKey] = server;
        _parameters.Projects[ProjectName] = project;
        return server;
    }

    private void VerifyNotSaved()
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
