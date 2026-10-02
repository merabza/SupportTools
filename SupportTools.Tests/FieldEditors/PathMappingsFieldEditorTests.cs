using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.Cruders;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.FieldEditors;

public sealed class PathMappingsFieldEditorTests
{
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public PathMappingsFieldEditorTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    //the rules are a list with its own menu: they are not asked one by one when a record is created
    [Fact]
    public void Constructor_WhenCreated_IsNotAskedOnCreate()
    {
        // Act
        PathMappingsFieldEditor sut = CreateSut();

        // Assert
        Assert.False(sut.EnterFieldDataOnCreate);
    }

    [Fact]
    public void AddParameterEditMenuItem_WhenCalled_AddsItemThatOpensSubMenu()
    {
        // Arrange
        var menuSet = new CliMenuSet();
        var parametersEditor = new SupportToolsParametersEditor(new Mock<IApplication>().Object,
            new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object, _parameters, _parametersManager.Object);

        // Act
        CreateSut().AddParameterEditMenuItem(menuSet, parametersEditor);

        // Assert
        CliMenuItem menuItem = Assert.Single(CliMenuTestAccess.GetMenuItems(menuSet));
        Assert.Equal("Path Mappings", menuItem.MenuItemName);
        Assert.IsType<ParameterSubObjectFieldEditorCliMenuCommand>(menuItem.CliMenuCommand);
    }

    [Fact]
    public void GetSubMenu_WhenCalled_OpensPathMappingsListOfRecord()
    {
        // Arrange
        _parameters.PathMappings.Add(new PathMappingModel
        {
            CanonicalPrefix = @"D:\1WorkDotnet", LocalPrefix = "/home/u/1WorkDotnet"
        });

        // Act
        CliMenuSet subMenu = CreateSut().GetSubMenu(_parameters);

        // Assert
        Assert.Equal("Path Mappings", subMenu.Caption);
        Assert.Contains(@"D:\1WorkDotnet",
            CliMenuTestAccess.GetMenuItems(subMenu).Where(x => x.CliMenuCommand is ItemSubMenuCliMenuCommand)
                .Select(x => x.MenuItemName));
    }

    //a change made in the opened list is saved through the parameters manager of the editor
    [Fact]
    public async Task GetSubMenu_WhenListSaves_SavesRootParameters()
    {
        // Arrange
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Cruder listCruder = CliMenuTestAccess.GetListMenuCruder(CreateSut().GetSubMenu(_parameters));

        // Act
        await listCruder.Save("Path Mapping saved");

        // Assert
        _parametersManager.Verify(x => x.Save(_parameters, "Path Mapping saved", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void GetSubMenu_WhenRecordHasNoList_OpensEmptyList()
    {
        // Arrange
        var parameters = new SupportToolsParameters { PathMappings = null! };

        // Act
        CliMenuSet subMenu = CreateSut().GetSubMenu(parameters);

        // Assert
        Assert.Equal("Path Mappings", subMenu.Caption);
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(subMenu),
            x => x.CliMenuCommand is ItemSubMenuCliMenuCommand);
    }

    [Theory]
    [InlineData(0, "No path mappings")]
    [InlineData(1, "1 path mapping")]
    [InlineData(3, "3 path mappings")]
    public void GetValueStatus_WhenCalled_ReturnsNumberOfRules(int count, string expected)
    {
        // Arrange
        for (int i = 0; i < count; i++)
        {
            _parameters.PathMappings.Add(new PathMappingModel());
        }

        // Act
        string status = CreateSut().GetValueStatus(_parameters);

        // Assert
        Assert.Equal(expected, status);
    }

    [Fact]
    public void GetValueStatus_WhenRecordHasNoList_ReturnsNoPathMappings()
    {
        // Arrange
        var parameters = new SupportToolsParameters { PathMappings = null! };

        // Act
        string status = CreateSut().GetValueStatus(parameters);

        // Assert
        Assert.Equal("No path mappings", status);
    }

    private PathMappingsFieldEditor CreateSut()
    {
        return new PathMappingsFieldEditor(nameof(SupportToolsParameters.PathMappings), _parametersManager.Object);
    }
}
