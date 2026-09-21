using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using Xunit;

namespace SupportTools.Tests.Cruders;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class EditorConfigPatternsCruderTests : IDisposable
{
    private readonly EditorConfigTestEnvironment _env = new();

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public void Create_WhenCalled_UsesEditorConfigPatternsOfParameters()
    {
        // Act
        var sut = EditorConfigPatternsCruder.Create(new Mock<ILogger>().Object, _env.ParametersManager.Object);

        // Assert
        Assert.True(sut.ContainsRecordWithKey(EditorConfigTestEnvironment.PatternName));
        Assert.False(sut.ContainsRecordWithKey("React"));
    }

    [Fact]
    public void GetStatusFor_WhenPatternIsNotUsed_ReportsZero()
    {
        // Act
        string status = CreateSut().GetStatusFor("React");

        // Assert
        Assert.Equal("Usage count is: 0", status);
    }

    [Fact]
    public void GetStatusFor_WhenProjectsUsePattern_CountsThem()
    {
        // Arrange
        _env.AddProject("SecondCSharp", EditorConfigTestEnvironment.PatternName, null);
        _env.AddProject("ReactProject", "React", null);
        _env.AddProject("NoPattern", null, null);

        // Act
        string status = CreateSut().GetStatusFor(EditorConfigTestEnvironment.PatternName);

        // Assert
        Assert.Equal("Usage count is: 2", status);
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsPatternsFollowedByCheckAndUpdateCommands()
    {
        // Arrange
        string[] expected =
        [
            "New EditorConfig Pattern", EditorConfigTestEnvironment.PatternName, "Check .editorconfig Files",
            "Update .editorconfig Files"
        ];

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal("EditorConfig Patterns", listMenu.Caption);
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(listMenu);
        Assert.Equal(expected, menuItems.Take(expected.Length).Select(x => x.MenuItemName));
        Assert.IsType<CheckEditorConfigFilesCliMenuCommand>(menuItems[2].CliMenuCommand);
        Assert.IsType<UpdateEditorConfigFilesCliMenuCommand>(menuItems[3].CliMenuCommand);
    }

    [Fact]
    public void FillDetailsSubMenu_WhenCalled_AddsRecordNameEditorFollowedByApplyCommand()
    {
        // Arrange
        var detailsMenu = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(detailsMenu, EditorConfigTestEnvironment.PatternName);

        // Assert
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(detailsMenu);
        Assert.Equal(2, menuItems.Count);
        Assert.Equal("Record Name", menuItems[0].MenuItemName);
        Assert.IsType<ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand>(menuItems[1].CliMenuCommand);
    }

    [Fact]
    public async Task FillDetailsSubMenu_WhenApplyCommandRuns_AppliesThePatternOfTheOpenedRecord()
    {
        // Arrange
        _env.Parameters.EditorConfigPatterns.Add("React");
        _env.AddProject("NoPattern", null, null);
        var detailsMenu = new CliMenuSet("Details");
        CreateSut().FillDetailsSubMenu(detailsMenu, "React");
        CliMenuCommand applyCommand = CliMenuTestAccess.GetMenuItems(detailsMenu)[1].CliMenuCommand;

        // Act
        await CliMenuTestAccess.InvokeRunBody(applyCommand);

        // Assert
        Assert.Equal("React", _env.Parameters.Projects["NoPattern"].EditorConfigPatternName);
    }

    private EditorConfigPatternsCruder CreateSut()
    {
        return new EditorConfigPatternsCruder(new Mock<ILogger>().Object, _env.ParametersManager.Object,
            _env.Parameters.EditorConfigPatterns);
    }
}
