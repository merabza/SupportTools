using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ReactAppTypeCruderTests : IDisposable
{
    private const string TemplateKey = "FakeReactApp";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public ReactAppTypeCruderTests()
    {
        _parameters.ReactAppTemplates[TemplateKey] = "FakeTemplate";
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
    public void Constructor_WhenCalled_NamesTheReactAppTypeRecords()
    {
        // Act
        var sut = new ReactAppTypeCruder(_logger.Object, _parametersManager.Object, []);

        // Assert
        Assert.Equal("React App Type", sut.CrudName);
        Assert.Equal("React App Types", sut.CrudNamePlural);
    }

    [Fact]
    public void Constructor_WhenCalled_EditsTheGivenDictionary()
    {
        // Arrange
        Dictionary<string, string> templates = new() { ["OtherReactApp"] = "OtherTemplate" };

        // Act
        var sut = new ReactAppTypeCruder(_logger.Object, _parametersManager.Object, templates);

        // Assert
        Assert.True(sut.ContainsRecordWithKey("OtherReactApp"));
        Assert.False(sut.ContainsRecordWithKey(TemplateKey));
        Assert.Equal("OtherTemplate", sut.GetStatusFor("OtherReactApp"));
    }

    [Fact]
    public async Task Create_WhenCalled_EditsTheReactAppTemplatesOfTheParameters()
    {
        // Arrange
        ReactAppTypeCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "NewReactApp", new TextItemData { Text = "NewTemplate" });

        // Assert
        Assert.True(sut.ContainsRecordWithKey(TemplateKey));
        Assert.Equal("NewTemplate", _parameters.ReactAppTemplates["NewReactApp"]);
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsTheTypesAndOffersToReCreateAllReactApps()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        List<CliMenuCommand> commands = GetCommands(listMenu);
        Assert.Equal([TemplateKey], commands.OfType<ItemSubMenuCliMenuCommand>().Select(x => x.Name));
        Assert.Single(commands.OfType<ReCreateAllReactAppsByTemplatesCliMenuCommand>());
    }

    [Fact]
    public void FillDetailsSubMenu_WhenTypeExists_OffersToReCreateItsReactApp()
    {
        // Arrange
        var itemMenu = new CliMenuSet(TemplateKey);

        // Act
        CreateSut().FillDetailsSubMenu(itemMenu, TemplateKey);

        // Assert
        List<CliMenuCommand> commands = GetCommands(itemMenu);
        Assert.Single(commands.OfType<ReCreateReactAppByTemplateNameCliMenuCommand>());
        //the menu items of the base cruder stay in front of it
        Assert.IsType<RecordKeyEditorCliMenuCommand>(commands[0]);
        Assert.IsType<ReCreateReactAppByTemplateNameCliMenuCommand>(commands[^1]);
    }

    [Fact]
    public void FillDetailsSubMenu_WhenTypeDoesNotExist_AddsNothing()
    {
        // Arrange
        var itemMenu = new CliMenuSet("Missing");

        // Act
        CreateSut().FillDetailsSubMenu(itemMenu, "Missing");

        // Assert
        Assert.Empty(GetCommands(itemMenu));
        Assert.Contains("React App Type with Name Missing is not exists.", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    private ReactAppTypeCruder CreateSut()
    {
        return ReactAppTypeCruder.Create(_logger.Object, _parametersManager.Object);
    }

    private static List<CliMenuCommand> GetCommands(CliMenuSet menuSet)
    {
        return [.. CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand)];
    }
}
