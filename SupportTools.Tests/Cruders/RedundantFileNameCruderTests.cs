using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class RedundantFileNameCruderTests
{
    private const string ProjectName = "FakeProject";

    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly ProjectModel _project = new() { RedundantFileNames = ["Readme.txt"] };

    public RedundantFileNameCruderTests()
    {
        _parameters.Projects[ProjectName] = _project;
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheRedundantFileNameRecords()
    {
        // Act
        RedundantFileNameCruder sut = CreateSut(ProjectName);

        // Assert
        Assert.Equal("Redundant File Name", sut.CrudName);
        Assert.Equal("Redundant File Names", sut.CrudNamePlural);
    }

    //a file name has no fields to edit one after another; the name itself is the record key
    [Fact]
    public void GetItemMenu_WhenCalled_OffersNoSequentialEditButKeepsTheRecordName()
    {
        // Act
        CliMenuSet itemMenu = CreateSut(ProjectName).GetItemMenu("Readme.txt");

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.CliMenuCommand)];
        Assert.Empty(commands.OfType<EditItemAllFieldsInSequenceCliMenuCommand>());
        Assert.Single(commands.OfType<RecordKeyEditorCliMenuCommand>());
    }

    [Fact]
    public async Task AddRecordWithKey_WhenProjectExists_AddsTheNameToTheProject()
    {
        // Arrange
        RedundantFileNameCruder sut = CreateSut(ProjectName);

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "Notes.txt", new TextItemData());

        // Assert
        Assert.Equal(["Readme.txt", "Notes.txt"], _project.RedundantFileNames);
        Assert.True(sut.ContainsRecordWithKey("Readme.txt"));
    }

    [Fact]
    public void ContainsRecordWithKey_WhenProjectDoesNotExist_FindsNoNames()
    {
        // Arrange
        RedundantFileNameCruder sut = CreateSut("MissingProject");

        // Act
        bool result = sut.ContainsRecordWithKey("Readme.txt");

        // Assert
        Assert.False(result);
        Assert.Empty(CliMenuTestAccess.GetCrudersDictionary(sut));
    }

    private RedundantFileNameCruder CreateSut(string projectName)
    {
        return new RedundantFileNameCruder(_parametersManager.Object, projectName);
    }
}
