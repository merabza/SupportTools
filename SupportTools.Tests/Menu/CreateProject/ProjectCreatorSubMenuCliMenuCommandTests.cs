using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.CreateProject;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Menu.CreateProject;

public sealed class ProjectCreatorSubMenuCliMenuCommandTests
{
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public ProjectCreatorSubMenuCliMenuCommandTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    //the menu links of the usages (DatabaseServerConnectionUsages, ServerUsages) name this main menu item
    [Fact]
    public void Constructor_WhenCreated_OpensTheProjectCreatorMenu()
    {
        // Act
        ProjectCreatorSubMenuCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Project Creator", sut.Name);
        Assert.Equal(EMenuAction.LoadSubMenu, sut.MenuActionOnBodySuccess);
    }

    //a parameters file without the project creator part gets an empty one in memory
    [Fact]
    public void GetSubMenu_WhenProjectCreatorParametersAreMissing_CreatesThem()
    {
        // Act
        CliMenuSet menu = CreateSut().GetSubMenu();

        // Assert
        Assert.NotNull(_parameters.AppProjectCreatorAllParameters);
        Assert.Equal(ProjectCreatorSubMenuCliMenuCommand.MenuName, menu.Caption);
    }

    [Fact]
    public void GetSubMenu_WhenProjectCreatorParametersExist_EditsThem()
    {
        // Arrange
        var creatorParameters = new AppProjectCreatorAllParameters { ProductionServerName = "Srv" };
        _parameters.AppProjectCreatorAllParameters = creatorParameters;

        // Act
        CliMenuSet menu = CreateSut().GetSubMenu();

        // Assert
        Assert.Same(creatorParameters, _parameters.AppProjectCreatorAllParameters);
        CliMenuCommand productionServerName = menu.GetMenuItemWithName("Production Server Name")!.CliMenuCommand;
        productionServerName.CountStatus();
        Assert.Equal("Srv", productionServerName.StatusString);
    }

    //the creator parameters, the class creators, the test projects, a new template and the templates in name order
    [Fact]
    public void GetSubMenu_WhenCalled_ListsParametersCommandsAndTemplatesInMenuOrder()
    {
        // Arrange
        _parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            Templates = { ["Beta"] = new TemplateModel(), ["Alpha"] = new TemplateModel() }
        };

        // Act
        CliMenuSet menu = CreateSut().GetSubMenu();

        // Assert
        Assert.Equal(
        [
            "Edit Parameters in sequence", "Indent Size", "Projects Folder Path Real", "Secrets Folder Path Real",
            "Developer Db Connection Name", "Database Exchange File Storage Name", "Use Smart Schema",
            "Production Server Name", "Production Environment Name", "Create Class Creator",
            "Create binary file Class Creator", "Create All Template Test Projects", "New Project Creator Template",
            "Alpha", "Beta", "Exit to level up menu"
        ], CliMenuTestAccess.GetMenuItems(menu).Select(x => x.MenuItemName));
    }

    private ProjectCreatorSubMenuCliMenuCommand CreateSut()
    {
        return new ProjectCreatorSubMenuCliMenuCommand(new Mock<IApplication>().Object, new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object);
    }
}
