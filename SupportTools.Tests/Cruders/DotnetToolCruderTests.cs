using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.FieldEditors;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class DotnetToolCruderTests
{
    private const string ToolKey = "FakeTool";

    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly DotnetToolData _tool = new()
    {
        PackageId = "Fake.Tool.Package",
        InstalledVersion = "1.0.0",
        LatestVersion = "2.0.0",
        CommandName = "fake-tool",
        Description = "Fake tool"
    };

    public DotnetToolCruderTests()
    {
        _parameters.DotnetTools[ToolKey] = _tool;
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCalled_EditsEveryFieldOfATool()
    {
        // Act
        DotnetToolCruder sut = CreateSut();

        // Assert
        Assert.Equal("Dotnet Tool", sut.CrudName);
        Assert.Equal("Dotnet Tools", sut.CrudNamePlural);
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(sut);
        Assert.Equal(
        [
            nameof(DotnetToolData.PackageId), nameof(DotnetToolData.InstalledVersion),
            nameof(DotnetToolData.LatestVersion), nameof(DotnetToolData.MaxVersion),
            nameof(DotnetToolData.CommandName), nameof(DotnetToolData.Description)
        ], fieldEditors.Select(x => x.PropertyName));
        Assert.All(fieldEditors, x => Assert.IsType<TextFieldEditor>(x));
    }

    //building the list used to replace the stored versions with the installed and the latest ones without saving them.
    //The list shows the stored data now; Check Dotnet Tools Versions refreshes and saves it
    [Fact]
    public void GetListMenu_WhenCalled_DoesNotChangeTheToolsData()
    {
        // Act
        CreateSut().GetListMenu();

        // Assert
        Assert.Equal("1.0.0", _tool.InstalledVersion);
        Assert.Equal("2.0.0", _tool.LatestVersion);
        Assert.Equal("fake-tool", _tool.CommandName);
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetListMenu_WhenCalled_OffersToCheckAndUpdateAllTools()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand)];
        Assert.Single(commands.OfType<CheckDotnetToolsVersionsCliMenuCommand>());
        Assert.Single(commands.OfType<UpdateAllToolsToLatestVersionCliMenuCommand>());
    }

    [Fact]
    public void GetItemMenu_WhenCalled_OffersToCheckAndUpdateTheTool()
    {
        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu(ToolKey);

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.CliMenuCommand)];
        Assert.Single(commands.OfType<CheckOneDotnetToolVersionsCliMenuCommand>());
        Assert.Single(commands.OfType<UpdateOneToolToLatestVersionCliMenuCommand>());
        //the field editors of the record are still there
        Assert.Single(commands.OfType<RecordKeyEditorCliMenuCommand>());
    }

    //the status shows the installed version, the latest one only when it differs, and the description
    [Theory]
    [InlineData("1.0.0", "2.0.0", " 1.0.0 (2.0.0) Fake tool ")]
    [InlineData("2.0.0", "2.0.0", " 2.0.0  Fake tool ")]
    public void GetStatusFor_WhenToolExists_ShowsItsVersionsAndDescription(string installedVersion,
        string latestVersion, string expected)
    {
        // Arrange
        _tool.InstalledVersion = installedVersion;
        _tool.LatestVersion = latestVersion;

        // Act
        string? status = CreateSut().GetStatusFor(ToolKey);

        // Assert
        Assert.Equal(expected, status);
    }

    [Fact]
    public void GetStatusFor_WhenToolDoesNotExist_ReturnsNoStatus()
    {
        // Act
        string? status = CreateSut().GetStatusFor("Missing");

        // Assert
        Assert.Null(status);
    }

    private DotnetToolCruder CreateSut()
    {
        return new DotnetToolCruder(_parametersManager.Object, _parameters.DotnetTools);
    }
}
