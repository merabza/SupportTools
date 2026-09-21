using System.Collections.Generic;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;

public sealed class SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategyTests
{
    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesSaveCommandForCurrentProject()
    {
        // Arrange
        var menuParameters = new SupportToolsMenuParameters { ProjectName = "MyProject" };
        var sut = new SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategy(menuParameters,
            new Mock<ILogger<SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategy>>().Object,
            new Mock<IParametersManager>().Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        var saveCommand = Assert.IsType<SaveEditorConfigAsNewTemplateCliMenuCommand>(command);
        Assert.Equal("MyProject", saveCommand.ParentMenuName);
    }

    //strategies are looked up by type name, so the project submenu shows the command only if its name is listed
    [Fact]
    public void ProjectSubMenuCommandFactoryStrategyNames_ListStrategyRightAfterGitMenus()
    {
        // Act
        List<string> strategyNames = MenuData.ProjectSubMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy), strategyNames[index - 1]);
    }
}
