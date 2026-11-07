using LibGitData.Models;
using LibGitWork.ToolCommandParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolCommandParameters;

public sealed class UpdateOutdatedPackagesParametersTests
{
    //the order of the update needs the types of the gits, so the gits come with the projects
    [Fact]
    public void Create_WhenCalled_TakesTheProjectsAndGitsOfTheParameters()
    {
        // Arrange
        var supportToolsParameters = new SupportToolsParameters();
        supportToolsParameters.Projects["P1"] = new ProjectModel();
        supportToolsParameters.Gits["GitA"] = new GitDataModel { GitIgnorePatternName = "CSharp" };

        // Act
        var result = UpdateOutdatedPackagesParameters.Create(supportToolsParameters, "G1", "P1");

        // Assert
        Assert.Same(supportToolsParameters.Projects, result.Projects);
        Assert.Same(supportToolsParameters.Gits, result.Gits);
        Assert.Equal("G1", result.ProjectGroupName);
        Assert.Equal("P1", result.ProjectName);
    }

    [Fact]
    public void CheckBeforeSave_WhenCalled_ReturnsTrue()
    {
        // Arrange
        var sut = UpdateOutdatedPackagesParameters.Create(new SupportToolsParameters(), null, null);

        // Act
        bool result = sut.CheckBeforeSave();

        // Assert
        Assert.True(result);
    }
}
