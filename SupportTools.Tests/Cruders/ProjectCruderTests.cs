using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportTools.FieldEditors;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class ProjectCruderTests
{
    private const string ProjectName = "MyProject";

    //the order is the order of the fields in the project editor menu
    private static readonly string[] ExpectedPropertyNames =
    [
        nameof(ProjectModel.ProjectType),
        nameof(ProjectModel.ProjectGroupName),
        nameof(ProjectModel.ProjectDescription),
        nameof(ProjectModel.MajorVersion),
        nameof(ProjectModel.MinorVersion),
        nameof(ProjectModel.UseAlternativeWebAgent),
        nameof(ProjectModel.UseLocalPackages),
        nameof(ProjectModel.ProgramArchiveDateMask),
        nameof(ProjectModel.ProgramArchiveExtension),
        nameof(ProjectModel.ParametersFileDateMask),
        nameof(ProjectModel.ParametersFileExtension),
        nameof(ProjectModel.ProjectFolderName),
        nameof(ProjectModel.SolutionFileName),
        nameof(ProjectModel.EditorConfigPatternName),
        nameof(ProjectModel.ProjectSecurityFolderPath),
        nameof(ProjectModel.MainProjectName),
        nameof(ProjectModel.ApiContractsProjectName),
        nameof(ProjectModel.SpaProjectName),
        nameof(ProjectModel.AppSetEnKeysJsonFileName),
        nameof(ProjectModel.KeyGuidPart),
        nameof(ProjectModel.UsesDatabase),
        nameof(ProjectModel.DevDatabaseParameters),
        nameof(ProjectModel.ProdCopyDatabaseParameters),
        nameof(ProjectModel.DbContextName),
        nameof(ProjectModel.DataSeederRulesByTableStartupProjectFilePath),
        nameof(ProjectModel.OldDataConvertorForDataSeeder),
        nameof(ProjectModel.MigrationStartupProjectFilePath),
        nameof(ProjectModel.MigrationProjectFilePath),
        nameof(ProjectModel.PairedDbObjectsResultFileName),
        nameof(ProjectModel.Endpoints),
        nameof(ProjectModel.RouteClasses),
        nameof(ProjectModel.FrontNpmPackageNames)
    ];

    private readonly SupportToolsParameters _parameters = new()
    {
        Projects = { [ProjectName] = new ProjectModel { RedundantFileNames = { "*.tmp", "*.bak" } } }
    };

    private readonly Mock<IParametersManager> _parametersManager = new();

    public ProjectCruderTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCreated_RegistersProjectFieldEditorsInMenuOrder()
    {
        // Act
        List<FieldEditor> fieldEditors = GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(ExpectedPropertyNames, fieldEditors.Select(x => x.PropertyName));
    }

    //a project must not be forced to have a main project, so its editor offers the (None) choice
    [Fact]
    public void Constructor_WhenCreated_OffersNoneOnlyForMainProjectName()
    {
        // Arrange
        string[] expected = [nameof(ProjectModel.MainProjectName)];
        FieldInfo useNoneField =
            typeof(GitProjectNameFieldEditor).GetField("_useNone", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Act
        List<FieldEditor> fieldEditors = GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(expected,
            fieldEditors.OfType<GitProjectNameFieldEditor>().Where(x => (bool)useNoneField.GetValue(x)!)
                .Select(x => x.PropertyName));
    }

    [Fact]
    public void Constructor_WhenCreated_NamesRecordsAsProjects()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal("Projects", listMenu.Caption);
        Assert.Equal("New Project", CliMenuTestAccess.GetMenuItems(listMenu)[0].MenuItemName);
    }

    [Fact]
    public void Create_WhenCalled_UsesProjectsOfParameters()
    {
        // Act
        var sut = ProjectCruder.Create(new Mock<IApplication>().Object, new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object);

        // Assert
        Assert.True(sut.ContainsRecordWithKey(ProjectName));
        Assert.False(sut.ContainsRecordWithKey("AnotherProject"));
    }

    //a new project uses local packages and the database unless the user turns them off
    [Theory]
    [InlineData(nameof(ProjectModel.UseLocalPackages))]
    [InlineData(nameof(ProjectModel.UsesDatabase))]
    public void SetDefault_WhenNewProjectIsCreated_TurnsBoolFieldOn(string propertyName)
    {
        // Arrange
        var project = new ProjectModel { UseLocalPackages = false, UsesDatabase = false };
        FieldEditor fieldEditor = GetFieldEditors(CreateSut()).Single(x => x.PropertyName == propertyName);

        // Act
        fieldEditor.SetDefault(project);

        // Assert
        Assert.True((bool)typeof(ProjectModel).GetProperty(propertyName)!.GetValue(project)!);
    }

    [Fact]
    public void GetDetailsSubMenu_WhenCalled_ListsRecordNameAndFieldsFollowedByRedundantFileNames()
    {
        // Act
        List<CliMenuCommand> commands = CreateSut().GetDetailsSubMenu(ProjectName);

        // Assert
        AssertDetails(commands);
    }

    [Fact]
    public void FillDetailsSubMenu_WhenCalled_ListsRecordNameAndFieldsFollowedByRedundantFileNames()
    {
        // Arrange
        var detailsMenu = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(detailsMenu, ProjectName);

        // Assert
        AssertDetails([.. CliMenuTestAccess.GetMenuItems(detailsMenu).Select(x => x.CliMenuCommand)]);
    }

    [Fact]
    public void GetDetailsSubMenu_WhenProjectDoesNotUseDatabase_HidesDatabaseFields()
    {
        // Arrange
        _parameters.Projects[ProjectName].UsesDatabase = false;

        // Act
        List<string> names = [.. CreateSut().GetDetailsSubMenu(ProjectName).Select(x => x.Name)];

        // Assert
        Assert.Contains(nameof(ProjectModel.UsesDatabase).SplitWithSpacesCamelParts(), names);
        Assert.Contains(nameof(ProjectModel.MainProjectName).SplitWithSpacesCamelParts(), names);
        Assert.DoesNotContain(nameof(ProjectModel.DevDatabaseParameters).SplitWithSpacesCamelParts(), names);
        Assert.DoesNotContain(nameof(ProjectModel.MigrationProjectFilePath).SplitWithSpacesCamelParts(), names);
        Assert.DoesNotContain(nameof(ProjectModel.PairedDbObjectsResultFileName).SplitWithSpacesCamelParts(), names);
    }

    //IsDbTools keeps the general and the database fields; UsesDatabase is hidden and ignored
    [Fact]
    public void GetDetailsSubMenu_WhenProjectIsDbTools_ShowsGeneralAndDatabaseFieldsOnly()
    {
        // Arrange
        _parameters.Projects[ProjectName] = new ProjectModel
        {
            ProjectType = EProjectType.IsDbTools, UsesDatabase = false, RedundantFileNames = { "*.tmp", "*.bak" }
        };
        string[] expected =
        [
            nameof(ProjectModel.ProjectType), nameof(ProjectModel.ProjectGroupName),
            nameof(ProjectModel.ProjectDescription), nameof(ProjectModel.UseLocalPackages),
            nameof(ProjectModel.ProjectFolderName), nameof(ProjectModel.SolutionFileName),
            nameof(ProjectModel.EditorConfigPatternName), nameof(ProjectModel.DevDatabaseParameters),
            nameof(ProjectModel.ProdCopyDatabaseParameters), nameof(ProjectModel.DbContextName),
            nameof(ProjectModel.DataSeederRulesByTableStartupProjectFilePath),
            nameof(ProjectModel.OldDataConvertorForDataSeeder),
            nameof(ProjectModel.MigrationStartupProjectFilePath), nameof(ProjectModel.MigrationProjectFilePath),
            nameof(ProjectModel.PairedDbObjectsResultFileName)
        ];

        // Act
        List<CliMenuCommand> commands = CreateSut().GetDetailsSubMenu(ProjectName);

        // Assert
        Assert.Equal(expected.Select(x => x.SplitWithSpacesCamelParts()),
            commands.Skip(1).Take(expected.Length).Select(x => x.Name));
        Assert.Equal(1 + expected.Length + 3, commands.Count);
    }

    //"Record Name", then one editor per field, then the redundant file names part: the "new" command and the masks
    private static void AssertDetails(List<CliMenuCommand> commands)
    {
        int fieldsCount = ExpectedPropertyNames.Length;
        Assert.Equal(1 + fieldsCount + 3, commands.Count);
        Assert.Equal("Record Name", commands[0].Name);

        CliMenuCommand newRedundantFileNameCommand = commands[1 + fieldsCount];
        Assert.IsType<NewItemCliMenuCommand>(newRedundantFileNameCommand);
        Assert.Equal("Create New Redundant File Name", newRedundantFileNameCommand.Name);
        Assert.Equal(ProjectName, newRedundantFileNameCommand.ParentMenuName);

        List<CliMenuCommand> maskCommands = commands[(2 + fieldsCount)..];
        Assert.Equal(["*.tmp", "*.bak"], maskCommands.Select(x => x.Name));
        Assert.All(maskCommands, x =>
        {
            Assert.IsType<ItemSubMenuCliMenuCommand>(x);
            Assert.Equal(ProjectName, x.ParentMenuName);
            Assert.True(x.NameIsStatus);
        });
    }

    private ProjectCruder CreateSut()
    {
        return new ProjectCruder(new Mock<IApplication>().Object, new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, _parameters.Projects);
    }

    //Cruder keeps its field editors in a protected field
    private static List<FieldEditor> GetFieldEditors(Cruder cruder)
    {
        FieldInfo fieldEditorsField =
            typeof(Cruder).GetField("FieldEditors", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<FieldEditor>)fieldEditorsField.GetValue(cruder)!;
    }
}
