using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ProjectCreatorSettingsMapperTests
{
    private const string CanonicalProjectsFolder = @"D:\1WorkDotnet\Projects";
    private const string CanonicalSecretsFolder = @"D:\1WorkDotnet\Secrets";
    private const string LinuxProjectsFolder = "/home/u/1WorkDotnet/Projects";
    private const string LinuxSecretsFolder = "/home/u/1WorkDotnet/Secrets";

    //the templates are a collection of their own and stay on the local object
    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalObjectOnWindows_RestoresTheOriginal()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.WindowsPathMapper();
        AppProjectCreatorAllParameters original = NewParameters(CanonicalProjectsFolder, CanonicalSecretsFolder);
        StsProjectCreatorSettingsDataModel contract = ProjectCreatorSettingsMapper.ToContract(original, pathMapper);
        var restored = new AppProjectCreatorAllParameters { Templates = { ["Console"] = new TemplateModel() } };

        // Act
        ProjectCreatorSettingsMapper.ApplyToLocal(contract, restored, pathMapper);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    [Fact]
    public void ToContract_WhenComputerIsLinux_SendsCanonicalPaths()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();

        // Act
        StsProjectCreatorSettingsDataModel result =
            ProjectCreatorSettingsMapper.ToContract(NewParameters(LinuxProjectsFolder, LinuxSecretsFolder),
                pathMapper);

        // Assert
        Assert.Equal(CanonicalProjectsFolder, result.ProjectsFolderPathReal);
        Assert.Equal(CanonicalSecretsFolder, result.SecretsFolderPathReal);
        Assert.Empty(pathMapper.Issues);
    }

    [Fact]
    public void ApplyToLocal_WhenComputerIsLinux_StoresLocalPaths()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        StsProjectCreatorSettingsDataModel contract = ProjectCreatorSettingsMapper.ToContract(
            NewParameters(CanonicalProjectsFolder, CanonicalSecretsFolder), MapperTestHelpers.WindowsPathMapper());
        var local = new AppProjectCreatorAllParameters();

        // Act
        ProjectCreatorSettingsMapper.ApplyToLocal(contract, local, pathMapper);

        // Assert
        Assert.Equal(LinuxProjectsFolder, local.ProjectsFolderPathReal);
        Assert.Equal(LinuxSecretsFolder, local.SecretsFolderPathReal);
        Assert.Empty(pathMapper.Issues);
    }

    [Fact]
    public void ToContract_WhenLocalObjectIsMissing_GivesAnEmptyContract()
    {
        // Act
        StsProjectCreatorSettingsDataModel result =
            ProjectCreatorSettingsMapper.ToContract(null, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(MapperTestHelpers.HashOf(new StsProjectCreatorSettingsDataModel()),
            MapperTestHelpers.HashOf(result));
    }

    [Fact]
    public void Normalize_WhenDriveLetterCaseDiffers_GivesTheSameHash()
    {
        // Arrange
        var lower = new StsProjectCreatorSettingsDataModel
        {
            ProjectsFolderPathReal = @"d:\1WorkDotnet\Projects", SecretsFolderPathReal = @"d:\1WorkDotnet\Secrets"
        };
        var upper = new StsProjectCreatorSettingsDataModel
        {
            ProjectsFolderPathReal = CanonicalProjectsFolder, SecretsFolderPathReal = CanonicalSecretsFolder
        };

        // Act
        string lowerHash = MapperTestHelpers.HashOf(ProjectCreatorSettingsMapper.Normalize(lower));
        string upperHash = MapperTestHelpers.HashOf(ProjectCreatorSettingsMapper.Normalize(upper));

        // Assert
        Assert.Equal(upperHash, lowerHash);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmptyOrNull_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsProjectCreatorSettingsDataModel
        {
            IndentSize = 4,
            FakeHostProjectName = "",
            ProjectsFolderPathReal = "",
            SecretsFolderPathReal = "",
            ProductionServerName = "",
            ProductionEnvironmentName = "",
            DeveloperDbConnectionName = "",
            DatabaseExchangeFileStorageName = "",
            UseSmartSchema = ""
        };

        // Act
        string emptyHash = MapperTestHelpers.HashOf(ProjectCreatorSettingsMapper.Normalize(empty));
        string missingHash = MapperTestHelpers.HashOf(
            ProjectCreatorSettingsMapper.Normalize(new StsProjectCreatorSettingsDataModel { IndentSize = 4 }));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }

    private static AppProjectCreatorAllParameters NewParameters(string projectsFolder, string secretsFolder)
    {
        return new AppProjectCreatorAllParameters
        {
            IndentSize = 4,
            FakeHostProjectName = "FakeHost",
            ProjectsFolderPathReal = projectsFolder,
            ProductionServerName = "dl360",
            ProductionEnvironmentName = "Production",
            SecretsFolderPathReal = secretsFolder,
            DeveloperDbConnectionName = "Dev",
            DatabaseExchangeFileStorageName = "exchange",
            UseSmartSchema = "Daily",
            Templates = { ["Console"] = new TemplateModel() }
        };
    }
}
