using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ProjectTemplateMapperTests
{
    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalRecord_RestoresTheOriginal()
    {
        // Arrange
        TemplateModel original = NewTemplate();
        StsProjectTemplateDataModel contract = ProjectTemplateMapper.ToContract("Api With React", original);
        var restored = new TemplateModel();

        // Act
        ProjectTemplateMapper.ApplyToLocal(contract, restored);

        // Assert
        Assert.Equal("Api", contract.SupportProjectType);
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    //the name may come from another client in another case
    [Fact]
    public void ApplyToLocal_WhenProjectTypeDiffersInCase_StoresTheType()
    {
        // Arrange
        var template = new TemplateModel();

        // Act
        ProjectTemplateMapper.ApplyToLocal(new StsProjectTemplateDataModel { Name = "T", SupportProjectType = "razor" },
            template);

        // Assert
        Assert.Equal(ESupportProjectType.Razor, template.SupportProjectType);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmptyOrNullOrTypeCaseDiffers_GivesTheSameHash()
    {
        // Arrange
        var local = new StsProjectTemplateDataModel
        {
            Name = "T", SupportProjectType = "Api", TestProjectName = "", TestProjectShortName = "",
            ReactTemplateName = ""
        };
        var server = new StsProjectTemplateDataModel { Name = "T", SupportProjectType = "api", Version = 2 };

        // Act
        string localHash = MapperTestHelpers.HashOf(ProjectTemplateMapper.Normalize(local));
        string serverHash = MapperTestHelpers.HashOf(ProjectTemplateMapper.Normalize(server));

        // Assert
        Assert.Equal(localHash, serverHash);
    }

    [Theory]
    [InlineData("ScaffoldSeeder", false)]
    [InlineData("Worker", true)]
    public void HasUnknownSupportProjectType_WhenCalled_FindsTypesUnknownToThisClient(string type, bool expected)
    {
        // Arrange
        var contract = new StsProjectTemplateDataModel { Name = "T", SupportProjectType = type };

        // Act
        bool result = ProjectTemplateMapper.HasUnknownSupportProjectType(contract);

        // Assert
        Assert.Equal(expected, result);
    }

    //every flag is true, so the round trip shows a flag that is not copied
    private static TemplateModel NewTemplate()
    {
        return new TemplateModel
        {
            SupportProjectType = ESupportProjectType.Api,
            TestProjectName = "TestApi",
            TestProjectShortName = "ta",
            UseDatabase = true,
            UseDbPartFolderForDatabaseProjects = true,
            UseMenu = true,
            UseHttps = true,
            UseReact = true,
            UseCarcass = true,
            UseIdentity = true,
            UseReCounter = true,
            UseSignalR = true,
            UseFluentValidation = true,
            ReactTemplateName = "TypeScript"
        };
    }
}
