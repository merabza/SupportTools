using System;
using System.Linq;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Models;

public sealed class ProjectDatabaseScopeTests
{
    [Fact]
    public void NewProject_UsesDatabase()
    {
        Assert.True(new ProjectModel().UsesDatabase);
    }

    [Theory]
    [InlineData(EProjectType.Standard, true, true)]
    [InlineData(EProjectType.Standard, false, false)]
    [InlineData(EProjectType.IsService, false, false)]
    [InlineData(EProjectType.IsDbTools, true, true)]
    [InlineData(EProjectType.IsDbTools, false, true)]
    public void ShowsDatabase_DependsOnTypeAndUsesDatabase(EProjectType projectType, bool usesDatabase, bool expected)
    {
        var project = new ProjectModel { ProjectType = projectType, UsesDatabase = usesDatabase };

        Assert.Equal(expected, project.ShowsDatabase());
    }

    [Fact]
    public void IsToolVisible_WhenProjectUsesDatabase_ShowsEveryTool()
    {
        var project = new ProjectModel { ProjectType = EProjectType.IsService };

        Assert.All(Enum.GetValues<EProjectTools>(), tool => Assert.True(project.IsToolVisible(tool)));
        Assert.All(Enum.GetValues<EProjectServerTools>(), tool => Assert.True(project.IsToolVisible(tool)));
    }

    [Fact]
    public void IsToolVisible_WhenProjectDoesNotUseDatabase_HidesDatabaseToolsOnly()
    {
        var project = new ProjectModel { ProjectType = EProjectType.IsService, UsesDatabase = false };

        Assert.Equal([EProjectTools.JetBrainsCleanupCode, EProjectTools.GenerateApiRoutes],
            Enum.GetValues<EProjectTools>().Where(project.IsToolVisible));
        Assert.DoesNotContain(EProjectServerTools.DevBaseToServerCopier,
            Enum.GetValues<EProjectServerTools>().Where(project.IsToolVisible));
        Assert.Contains(EProjectServerTools.ProgramUpdater,
            Enum.GetValues<EProjectServerTools>().Where(project.IsToolVisible));
    }

    //IsDbTools ignores UsesDatabase: it always shows the database tools and nothing else
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsToolVisible_WhenProjectIsDbTools_ShowsDatabaseToolsOnly(bool usesDatabase)
    {
        var project = new ProjectModel { ProjectType = EProjectType.IsDbTools, UsesDatabase = usesDatabase };

        Assert.Equal(Enum.GetValues<EProjectTools>().Where(x => x.IsDatabaseTool()),
            Enum.GetValues<EProjectTools>().Where(project.IsToolVisible));
        Assert.Equal(
            [
                EProjectServerTools.DevBaseToServerCopier, EProjectServerTools.ServerBaseToProdCopyCopier,
                EProjectServerTools.ServerBaseToServerCopier, EProjectServerTools.RestoreDatabaseFromFileStorageToNewDatabase
            ], Enum.GetValues<EProjectServerTools>().Where(project.IsToolVisible));
    }

    //every tool must be classified; an unclassified one throws
    [Fact]
    public void IsDatabaseTool_ClassifiesEveryTool()
    {
        Assert.Equal(8, Enum.GetValues<EProjectTools>().Count(x => x.IsDatabaseTool()));
        Assert.Equal(4, Enum.GetValues<EProjectServerTools>().Count(x => x.IsDatabaseTool()));
    }
}
