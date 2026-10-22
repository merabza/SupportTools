using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class ServerInfoCruderTests
{
    private readonly SupportToolsParameters _parameters = new()
    {
        Projects = { [ProjectTestData.Name] = ProjectTestData.NewProject() }
    };

    private readonly Mock<IParametersManager> _parametersManager = new();

    public ServerInfoCruderTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    //the copy is a command of the whole record, like the delete and the edit before it
    [Fact]
    public void GetItemMenu_WhenServerExists_OffersCopyRightAfterEditAllFieldsInSequence()
    {
        // Arrange
        string[] expected = ["Delete this record", "Edit All fields in sequence", "Copy Server..."];
        var sut = ServerInfoCruder.Create(new Mock<IApplication>().Object, new Mock<IServiceProvider>().Object,
            new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object, _parametersManager.Object,
            ProjectTestData.Name);

        // Act
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(sut.GetItemMenu(ProjectTestData.ProdKey));

        // Assert
        Assert.Equal(expected, menuItems.Take(3).Select(x => x.MenuItemName));
        Assert.IsType<CopyServerInfoCliMenuCommand>(menuItems[2].CliMenuCommand);
    }
}
