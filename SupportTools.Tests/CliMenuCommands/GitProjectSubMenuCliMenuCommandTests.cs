using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AppCliTools.CliMenu;
using LibGitData;
using LibGitWork.CliMenuCommands;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

public sealed class GitProjectSubMenuCliMenuCommandTests
{
    private const string GitProjectName = "MyGit";

    private readonly GitProjectSubMenuCliMenuCommand _sut = new(new Mock<ILogger>().Object,
        new Mock<IParametersManager>().Object, "TestProject", GitProjectName, EGitCol.Main);

    [Fact]
    public void Constructor_WhenCreated_NamesMenuItemAfterGitProject()
    {
        // Act
        string name = _sut.Name;

        // Assert
        Assert.Equal(GitProjectName, name);
    }

    [Fact]
    public void Constructor_WhenCreated_LoadsSubMenuOnRun()
    {
        // Act
        EMenuAction menuAction = _sut.MenuActionOnBodySuccess;

        // Assert
        Assert.Equal(EMenuAction.LoadSubMenu, menuAction);
    }

    [Fact]
    public void GetSubMenu_WhenCalled_ReturnsMenuCaptionedWithGitProjectName()
    {
        // Act
        CliMenuSet subMenu = _sut.GetSubMenu();

        // Assert
        Assert.Equal(GitProjectName, subMenu.Caption);
    }

    [Fact]
    public void GetSubMenu_WhenCalled_ListsCommandsInOrder()
    {
        // Arrange
        string[] expected = ["Delete Git Project", "Sync", "Save .gitignore as New Template", "Exit to Git menu"];

        // Act
        List<CliMenuItem> menuItems = GetMenuItems(_sut.GetSubMenu());

        // Assert
        Assert.Equal(expected, menuItems.Select(x => x.MenuItemName));
    }

    [Fact]
    public void GetSubMenu_WhenCalled_PlacesSaveGitIgnoreAsNewTemplateRightAfterSync()
    {
        // Act
        List<CliMenuItem> menuItems = GetMenuItems(_sut.GetSubMenu());

        // Assert
        int syncIndex = menuItems.FindIndex(x => x.CliMenuCommand is GitSyncCliMenuCommand);
        Assert.IsType<SaveGitIgnoreAsNewTemplateCliMenuCommand>(menuItems[syncIndex + 1].CliMenuCommand);
    }

    //CliMenuSet keeps its items (and their order) in private state only
    private static List<CliMenuItem> GetMenuItems(CliMenuSet menuSet)
    {
        PropertyInfo menuItemsProperty =
            typeof(CliMenuSet).GetProperty("MenuItems", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<CliMenuItem>)menuItemsProperty.GetValue(menuSet)!;
    }
}
