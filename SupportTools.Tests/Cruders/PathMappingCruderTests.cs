using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.FieldEditors;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class PathMappingCruderTests : IDisposable
{
    private const string WorkRoot = @"D:\1WorkDotnet";
    private const string LinuxWorkFolder = "/home/u/1WorkDotnet";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public PathMappingCruderTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_NamesThePathMappingRecords()
    {
        // Act
        PathMappingCruder sut = CreateSut();

        // Assert
        Assert.Equal("Path Mapping", sut.CrudName);
        Assert.Equal("Path Mappings", sut.CrudNamePlural);
    }

    //both prefixes are asked when a new rule is created
    [Fact]
    public void Constructor_WhenCreated_EditsCanonicalAndLocalPrefixOnCreate()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        Assert.Collection(fieldEditors, x =>
        {
            Assert.IsType<TextFieldEditor>(x);
            Assert.Equal(nameof(PathMappingModel.CanonicalPrefix), x.PropertyName);
            Assert.True(x.EnterFieldDataOnCreate);
        }, x =>
        {
            Assert.IsType<FolderPathFieldEditor>(x);
            Assert.Equal(nameof(PathMappingModel.LocalPrefix), x.PropertyName);
            Assert.True(x.EnterFieldDataOnCreate);
        });
    }

    [Fact]
    public void GetListMenu_WhenRulesExist_ListsCanonicalPrefixesWithLocalPrefixAsStatus()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, LinuxWorkFolder));
        _parameters.PathMappings.Add(Rule(@"D:\1WorkSecurity", "/home/u/1WorkSecurity"));
        PathMappingCruder sut = CreateSut();
        string[] expected = [WorkRoot, @"D:\1WorkSecurity"];

        // Act
        CliMenuSet listMenu = sut.GetListMenu();

        // Assert
        Assert.Equal(expected,
            CliMenuTestAccess.GetMenuItems(listMenu).Where(x => x.CliMenuCommand is ItemSubMenuCliMenuCommand)
                .Select(x => x.MenuItemName));
        Assert.Equal(LinuxWorkFolder, sut.GetStatusFor(WorkRoot));
    }

    [Fact]
    public void GetListMenu_WhenCreated_OffersSuggestPathMappingsCommand()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Single(CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand)
            .OfType<SuggestPathMappingsCliMenuCommand>());
    }

    //a broken file (duplicate or empty prefixes) must not break the menu: the keys are made unique
    [Fact]
    public void GetListMenu_WhenPrefixesAreDuplicateOrEmpty_GivesEveryRuleItsOwnKey()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, LinuxWorkFolder));
        _parameters.PathMappings.Add(Rule(WorkRoot, "/mnt/second"));
        _parameters.PathMappings.Add(Rule(null, "/mnt/empty"));
        PathMappingCruder sut = CreateSut();

        // Act
        CliMenuSet listMenu = sut.GetListMenu();

        // Assert
        Assert.Equal(3,
            CliMenuTestAccess.GetMenuItems(listMenu).Count(x => x.CliMenuCommand is ItemSubMenuCliMenuCommand));
        Assert.Equal("/mnt/second", sut.GetStatusFor($"{WorkRoot} (2)"));
        Assert.Equal("/mnt/empty", sut.GetStatusFor("(empty)"));
    }

    [Fact]
    public void GetListMenu_WhenPrefixIsRepeatedThreeTimes_NumbersTheRepeatedKeys()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, "/mnt/first"));
        _parameters.PathMappings.Add(Rule(WorkRoot, "/mnt/second"));
        _parameters.PathMappings.Add(Rule(WorkRoot, "/mnt/third"));
        PathMappingCruder sut = CreateSut();

        // Act
        string? first = sut.GetStatusFor(WorkRoot);
        string? second = sut.GetStatusFor($"{WorkRoot} (2)");
        string? third = sut.GetStatusFor($"{WorkRoot} (3)");

        // Assert
        Assert.Equal("/mnt/first", first);
        Assert.Equal("/mnt/second", second);
        Assert.Equal("/mnt/third", third);
    }

    [Fact]
    public void GetStatusFor_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, LinuxWorkFolder));

        // Act
        string? result = CreateSut().GetStatusFor(@"D:\1WorkSecurity");

        // Assert
        Assert.Null(result);
    }

    //the key comes from the canonical prefix, so the item menu has no separate record name editor
    [Fact]
    public void GetItemMenu_WhenCalled_OffersNoRecordNameEditor()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, LinuxWorkFolder));

        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu(WorkRoot);

        // Assert
        Assert.Empty(CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.CliMenuCommand)
            .OfType<RecordKeyEditorCliMenuCommand>());
    }

    [Fact]
    public void ContainsRecordWithKey_WhenChecked_FindsRulesByCanonicalPrefix()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, LinuxWorkFolder));
        PathMappingCruder sut = CreateSut();

        // Act
        bool existing = sut.ContainsRecordWithKey(WorkRoot);
        bool missing = sut.ContainsRecordWithKey(@"D:\1WorkSecurity");

        // Assert
        Assert.True(existing);
        Assert.False(missing);
    }

    [Fact]
    public async Task AddRecordWithKey_WhenCalled_AppendsTheRuleToParameters()
    {
        // Arrange
        PathMappingModel rule = Rule(WorkRoot, LinuxWorkFolder);

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(CreateSut(), Guid.NewGuid().ToString(), rule);

        // Assert
        Assert.Same(rule, Assert.Single(_parameters.PathMappings));
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenKeyIsOfDuplicate_RemovesThatRule()
    {
        // Arrange
        PathMappingModel first = Rule(WorkRoot, LinuxWorkFolder);
        _parameters.PathMappings.Add(first);
        _parameters.PathMappings.Add(Rule(WorkRoot, "/mnt/second"));

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), $"{WorkRoot} (2)");

        // Assert
        Assert.Same(first, Assert.Single(_parameters.PathMappings));
    }

    [Fact]
    public void CreateNewItem_WhenCalled_ReturnsEmptyPathMapping()
    {
        // Act
        var item = Assert.IsType<PathMappingModel>(CliMenuTestAccess.InvokeCreateNewItem(CreateSut()));

        // Assert
        Assert.Null(item.CanonicalPrefix);
        Assert.Null(item.LocalPrefix);
    }

    [Fact]
    public void CheckValidation_WhenItemIsNotPathMapping_ReturnsFalse()
    {
        // Act
        bool result = CreateSut().CheckValidation(new ItemData());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CheckValidation_WhenRuleIsComplete_ReturnsTrue()
    {
        // Act
        bool result = CreateSut().CheckValidation(Rule(WorkRoot, LinuxWorkFolder));

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null, LinuxWorkFolder)]
    [InlineData("  ", LinuxWorkFolder)]
    [InlineData(WorkRoot, null)]
    [InlineData(WorkRoot, "")]
    public void CheckValidation_WhenPrefixIsMissing_ReportsItAndReturnsFalse(string? canonicalPrefix,
        string? localPrefix)
    {
        // Act
        bool result = CreateSut().CheckValidation(Rule(canonicalPrefix, localPrefix));

        // Assert
        Assert.False(result);
        Assert.Contains("Canonical prefix and local prefix must be specified", ConsoleText(), StringComparison.Ordinal);
    }

    //the fields are easy to swap: a Linux path is not a canonical prefix
    [Fact]
    public void CheckValidation_WhenCanonicalPrefixIsNotWindowsPath_ReportsItAndReturnsFalse()
    {
        // Act
        bool result = CreateSut().CheckValidation(Rule(LinuxWorkFolder, WorkRoot));

        // Assert
        Assert.False(result);
        Assert.Contains($"Canonical prefix {LinuxWorkFolder} is not a Windows absolute path", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(WorkRoot)]
    [InlineData(@"d:\1workdotnet\")]
    public void CheckValidation_WhenOtherRuleHasSameCanonicalPrefix_ReportsItAndReturnsFalse(string canonicalPrefix)
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, LinuxWorkFolder));

        // Act
        bool result = CreateSut().CheckValidation(Rule(canonicalPrefix, "/mnt/other"));

        // Assert
        Assert.False(result);
        Assert.Contains($"Path mapping for {canonicalPrefix} already exists", ConsoleText(), StringComparison.Ordinal);
    }

    //a rule that is edited in place is already in the list and must not count as its own duplicate
    [Fact]
    public void CheckValidation_WhenRuleIsAlreadyInList_ReturnsTrue()
    {
        // Arrange
        PathMappingModel rule = Rule(WorkRoot, LinuxWorkFolder);
        _parameters.PathMappings.Add(rule);

        // Act
        bool result = CreateSut().CheckValidation(rule);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Save_WhenCalled_SavesRootParametersWithMessage()
    {
        // Act
        bool result = await CreateSut().Save("Path Mapping saved");

        // Assert
        Assert.True(result);
        _parametersManager.Verify(x => x.Save(_parameters, "Path Mapping saved", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private PathMappingCruder CreateSut()
    {
        return new PathMappingCruder(_parametersManager.Object, _parameters.PathMappings);
    }

    private static PathMappingModel Rule(string? canonicalPrefix, string? localPrefix)
    {
        return new PathMappingModel { CanonicalPrefix = canonicalPrefix, LocalPrefix = localPrefix };
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }
}
