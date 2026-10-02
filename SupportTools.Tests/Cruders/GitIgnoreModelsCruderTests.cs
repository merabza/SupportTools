using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.CliMenuCommands.GitIgnoreFileTypes;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class GitIgnoreModelsCruderTests
{
    private const string PatternName = "DotnetPattern";

    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public GitIgnoreModelsCruderTests()
    {
        _parameters.GitIgnorePatterns.Add(PatternName);
        _parameters.Gits["MainGit"] = new GitDataModel { GitIgnorePatternName = PatternName };
        _parameters.Gits["SeederGit"] = new GitDataModel { GitIgnorePatternName = PatternName };
        _parameters.Gits["OtherGit"] = new GitDataModel { GitIgnorePatternName = "OtherPattern" };
        _parameters.Gits["NoPatternGit"] = new GitDataModel();
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheGitIgnoreModelRecords()
    {
        // Act
        var sut = new GitIgnoreModelsCruder(_logger.Object, _httpClientFactory.Object, _parametersManager.Object, []);

        // Assert
        Assert.Equal("GitIgnore Model", sut.CrudName);
        Assert.Equal("GitIgnore Models", sut.CrudNamePlural);
    }

    [Fact]
    public void Constructor_WhenCalled_EditsTheGivenList()
    {
        // Act
        var sut = new GitIgnoreModelsCruder(_logger.Object, _httpClientFactory.Object, _parametersManager.Object,
            ["OtherPattern"]);

        // Assert
        Assert.True(sut.ContainsRecordWithKey("OtherPattern"));
        Assert.False(sut.ContainsRecordWithKey(PatternName));
    }

    [Fact]
    public async Task Create_WhenCalled_EditsTheGitIgnorePatternsOfTheParameters()
    {
        // Arrange
        GitIgnoreModelsCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "NodePattern", new TextItemData());

        // Assert
        Assert.True(sut.ContainsRecordWithKey(PatternName));
        Assert.Equal([PatternName, "NodePattern"], _parameters.GitIgnorePatterns);
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsThePatternsAndOffersTheGitIgnoreFilesCommands()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        List<CliMenuCommand> commands = GetCommands(listMenu);
        Assert.Equal([PatternName], commands.OfType<ItemSubMenuCliMenuCommand>().Select(x => x.Name));
        Assert.Single(commands.OfType<CheckGitIgnoreFilesCliMenuCommand>());
        Assert.Single(commands.OfType<UpdateGitIgnoreFilesCliMenuCommand>());
        Assert.Single(commands.OfType<GenerateStandardGitignoreFilesCliMenuCommand>());
        Assert.Single(commands.OfType<SyncGitignoreFilesCliMenuCommand>());
    }

    [Fact]
    public void GetItemMenu_WhenCalled_OffersToApplyThePatternToTheProjectsWithoutOne()
    {
        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu(PatternName);

        // Assert
        List<CliMenuCommand> commands = GetCommands(itemMenu);
        Assert.Single(commands.OfType<ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand>());
        //the record name editor of the base cruder is still there
        Assert.Single(commands.OfType<RecordKeyEditorCliMenuCommand>());
    }

    [Fact]
    public void GetStatusFor_WhenNoProjectUsesThePattern_CountsNoUsage()
    {
        // Act
        string status = CreateSut().GetStatusFor(PatternName);

        // Assert
        Assert.Equal("Usage count is: 0", status);
    }

    //every listed git of both git collections of every project counts; the same git listed twice counts twice
    [Fact]
    public void GetStatusFor_WhenGitsOfBothCollectionsUseThePattern_CountsEveryListing()
    {
        // Arrange
        _parameters.Projects["FirstProject"] = new ProjectModel
        {
            GitProjectNames = ["MainGit", "OtherGit", "NoPatternGit"], ScaffoldSeederGitProjectNames = ["SeederGit"]
        };
        _parameters.Projects["SecondProject"] = new ProjectModel
        {
            GitProjectNames = ["MainGit"], ScaffoldSeederGitProjectNames = ["MainGit"]
        };

        // Act
        string status = CreateSut().GetStatusFor(PatternName);

        // Assert
        Assert.Equal("Usage count is: 4", status);
    }

    [Fact]
    public void GetStatusFor_WhenListedGitIsNotRegistered_SkipsIt()
    {
        // Arrange
        _parameters.Projects["FirstProject"] = new ProjectModel
        {
            GitProjectNames = ["MissingGit", "MainGit"], ScaffoldSeederGitProjectNames = ["MissingGit"]
        };

        // Act
        string status = CreateSut().GetStatusFor(PatternName);

        // Assert
        Assert.Equal("Usage count is: 1", status);
    }

    [Fact]
    public void GetStatusFor_WhenPatternNameDiffersInCase_DoesNotCountIt()
    {
        // Arrange
        _parameters.Projects["FirstProject"] = new ProjectModel { GitProjectNames = ["MainGit"] };

        // Act
        string status = CreateSut().GetStatusFor(PatternName.ToUpperInvariant());

        // Assert
        Assert.Equal("Usage count is: 0", status);
    }

    //a parameters file may hold null instead of a git list
    [Fact]
    public void GetStatusFor_WhenGitListIsNull_CountsTheOtherList()
    {
        // Arrange
        _parameters.Projects["FirstProject"] = new ProjectModel
        {
            GitProjectNames = null!, ScaffoldSeederGitProjectNames = ["SeederGit"]
        };
        _parameters.Projects["SecondProject"] = new ProjectModel
        {
            GitProjectNames = ["MainGit"], ScaffoldSeederGitProjectNames = null!
        };

        // Act
        string status = CreateSut().GetStatusFor(PatternName);

        // Assert
        Assert.Equal("Usage count is: 2", status);
    }

    private GitIgnoreModelsCruder CreateSut()
    {
        return GitIgnoreModelsCruder.Create(_logger.Object, _httpClientFactory.Object, _parametersManager.Object);
    }

    private static List<CliMenuCommand> GetCommands(CliMenuSet menuSet)
    {
        return [.. CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand)];
    }
}
