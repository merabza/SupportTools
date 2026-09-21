using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.DeleteProject;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList;

//CliMenuSetFactory silently skips strategy names that are not registered, so only the strategies of the commands that
//ProjectSubMenuCliMenuCommand shows conditionally are registered here, plus DeleteProject as an always shown one
public sealed class ProjectSubMenuCliMenuCommandTests : IDisposable
{
    private const string ProjectName = "MyProject";
    private const string SolutionFileName = @"C:\Projects\MyProject\MyProject\MyProject.slnx";

    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly SupportToolsParameters _parameters = new();
    private readonly ParametersManager _parametersManager;
    private readonly ServiceProvider _serviceProvider;

    public ProjectSubMenuCliMenuCommandTests()
    {
        //some strategies cast IParametersManager to ParametersManager, so a mock cannot be used
        _parametersManager = new ParametersManager(null, _parameters);

        _serviceProvider = new ServiceCollection().AddLogging().AddSingleton(new Mock<IHttpClientFactory>().Object)
            .AddSingleton(_menuParameters).AddSingleton<IParametersManager>(_parametersManager)
            .AddTransient<IMenuCommandFactoryStrategy, DeleteProjectCliMenuCommandFactoryStrategy>()
            .AddTransient<IMenuCommandFactoryStrategy, OpenByVisualStudioCliMenuCommandFactoryStrategy>()
            .AddTransient<IMenuCommandFactoryStrategy, CheckPackageSolutionCliMenuCommandFactoryStrategy>()
            .AddTransient<IMenuCommandFactoryStrategy, BuildPackageCliMenuCommandFactoryStrategy>()
            .AddTransient<IMenuCommandFactoryStrategy, PackageDistributionCliMenuCommandFactoryStrategy>()
            .AddTransient<IMenuCommandFactoryStrategy, GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy>()
            .AddTransient<IMenuCommandFactoryStrategy, SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategy>()
            .BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_NamesMenuItemAfterProjectAndLoadsSubMenuOnRun()
    {
        // Act
        ProjectSubMenuCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal(ProjectName, sut.Name);
        Assert.Equal(EMenuAction.LoadSubMenu, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public void GetSubMenu_WhenCalled_ReturnsMenuCaptionedWithProjectName()
    {
        // Arrange
        RegisterProject(new ProjectModel());

        // Act
        CliMenuSet? subMenu = CreateSut().GetSubMenu();

        // Assert
        Assert.Equal(ProjectName, subMenu?.Caption);
    }

    //the strategies read the current project from SupportToolsMenuParameters when they create their commands
    [Fact]
    public void GetSubMenu_WhenCalled_MakesProjectCurrentForCreatedCommands()
    {
        // Arrange
        RegisterProject(new ProjectModel());
        _menuParameters.ProjectName = "AnotherProject";

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Equal(ProjectName, _menuParameters.ProjectName);
        Assert.Equal(ProjectName, commands.OfType<DeleteProjectCliMenuCommand>().Single().ParentMenuName);
    }

    [Fact]
    public void GetSubMenu_WhenProjectIsPackage_ShowsPackageCommands()
    {
        // Arrange
        RegisterProject(new ProjectModel { ProjectType = EProjectType.IsPackage });

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Single(commands.OfType<CheckPackageSolutionCliMenuCommand>());
        Assert.Single(commands.OfType<BuildPackageCliMenuCommand>());
        Assert.Single(commands.OfType<PackageDistributionCliMenuCommand>());
    }

    [Theory]
    [InlineData(EProjectType.Standard)]
    [InlineData(EProjectType.IsService)]
    public void GetSubMenu_WhenProjectIsNotPackage_HidesPackageCommands(EProjectType projectType)
    {
        // Arrange
        RegisterProject(new ProjectModel { ProjectType = projectType });

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Empty(commands.OfType<CheckPackageSolutionCliMenuCommand>());
        Assert.Empty(commands.OfType<BuildPackageCliMenuCommand>());
        Assert.Empty(commands.OfType<PackageDistributionCliMenuCommand>());
    }

    [Fact]
    public void GetSubMenu_WhenProjectHasScaffoldSeederProjectName_ShowsScaffoldSeederGits()
    {
        // Arrange
        RegisterProject(new ProjectModel { ScaffoldSeederProjectName = "Seeder" });

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Single(commands.OfType<GitSubMenuCliMenuCommand>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSubMenu_WhenProjectHasNoScaffoldSeederProjectName_HidesScaffoldSeederGits(
        string? scaffoldSeederProjectName)
    {
        // Arrange
        RegisterProject(new ProjectModel { ScaffoldSeederProjectName = scaffoldSeederProjectName });

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Empty(commands.OfType<GitSubMenuCliMenuCommand>());
    }

    [Fact]
    public void GetSubMenu_WhenProjectHasSolutionFile_ShowsOpenByVisualStudioOnWindowsOnly()
    {
        // Arrange
        RegisterProject(new ProjectModel { SolutionFileName = SolutionFileName });

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Equal(OperatingSystem.IsWindows() ? 1 : 0, commands.OfType<OpenByVisualStudioCliMenuCommand>().Count());
    }

    [Fact]
    public void GetSubMenu_WhenProjectHasSolutionFile_ShowsSaveEditorConfigAsNewTemplate()
    {
        // Arrange
        RegisterProject(new ProjectModel { SolutionFileName = SolutionFileName });

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Single(commands.OfType<SaveEditorConfigAsNewTemplateCliMenuCommand>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSubMenu_WhenProjectHasNoSolutionFile_HidesSolutionCommands(string? solutionFileName)
    {
        // Arrange
        RegisterProject(new ProjectModel { SolutionFileName = solutionFileName });

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Empty(commands.OfType<OpenByVisualStudioCliMenuCommand>());
        Assert.Empty(commands.OfType<SaveEditorConfigAsNewTemplateCliMenuCommand>());
    }

    [Fact]
    public void GetSubMenu_WhenProjectIsNotRegistered_ShowsOnlyUnconditionalCommands()
    {
        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        //the last item is the escape command of the menu
        Assert.Equal(2, commands.Count);
        Assert.IsType<DeleteProjectCliMenuCommand>(commands[0]);
    }

    [Fact]
    public void GetSubMenu_WhenAllConditionsAreMet_ListsCommandsInMenuDataOrder()
    {
        // Arrange
        RegisterProject(new ProjectModel
        {
            ProjectType = EProjectType.IsPackage,
            SolutionFileName = SolutionFileName,
            ScaffoldSeederProjectName = "Seeder"
        });
        List<Type> expected =
        [
            typeof(DeleteProjectCliMenuCommand),
            typeof(OpenByVisualStudioCliMenuCommand),
            typeof(CheckPackageSolutionCliMenuCommand),
            typeof(BuildPackageCliMenuCommand),
            typeof(PackageDistributionCliMenuCommand),
            typeof(GitSubMenuCliMenuCommand),
            typeof(SaveEditorConfigAsNewTemplateCliMenuCommand)
        ];
        if (!OperatingSystem.IsWindows())
        {
            expected.Remove(typeof(OpenByVisualStudioCliMenuCommand));
        }

        // Act
        List<CliMenuCommand> commands = GetSubMenuCommands();

        // Assert
        Assert.Equal(expected, commands.Take(expected.Count).Select(x => x.GetType()));
    }

    [Fact]
    public void CountStatus_WhenProjectBuildWasNotChecked_ShowsNoStatus()
    {
        // Arrange
        ProjectSubMenuCliMenuCommand sut = CreateSut();

        // Act
        sut.CountStatus();

        // Assert
        Assert.Null(sut.StatusString);
        Assert.Null(sut.StatusColorParts);
    }

    [Fact]
    public void CountStatus_WhenProjectBuildWasChecked_ShowsCheckResult()
    {
        // Arrange
        _menuParameters.ProjectBuildCheckResults[ProjectName] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 2, 1);
        ProjectSubMenuCliMenuCommand sut = CreateSut();

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("BuildFailed, errors: 2, warnings: 1", sut.StatusString);
        Assert.Equal(["BuildFailed", "errors: 2", "warnings: 1"], sut.StatusColorParts?.Select(x => x.Text));
    }

    //results are kept per project: the result of another project must not leak into this one
    [Fact]
    public void CountStatus_WhenOnlyAnotherProjectWasChecked_ShowsNoStatus()
    {
        // Arrange
        _menuParameters.ProjectBuildCheckResults["AnotherProject"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.Success);
        ProjectSubMenuCliMenuCommand sut = CreateSut();

        // Act
        sut.CountStatus();

        // Assert
        Assert.Null(sut.StatusString);
        Assert.Null(sut.StatusColorParts);
    }

    private ProjectSubMenuCliMenuCommand CreateSut()
    {
        return new ProjectSubMenuCliMenuCommand(_serviceProvider, _parametersManager, ProjectName, _menuParameters);
    }

    private void RegisterProject(ProjectModel project)
    {
        _parameters.Projects[ProjectName] = project;
    }

    private List<CliMenuCommand> GetSubMenuCommands()
    {
        CliMenuSet subMenu = CreateSut().GetSubMenu()!;
        return [.. CliMenuTestAccess.GetMenuItems(subMenu).Select(x => x.CliMenuCommand)];
    }
}
