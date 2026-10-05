using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ReactAppTemplateMapperTests
{
    [Fact]
    public void ToLocal_WhenContractComesFromLocalRecord_GivesTheOriginalTemplate()
    {
        // Arrange
        StsReactAppTemplateDataModel contract =
            ReactAppTemplateMapper.ToContract("TypeScript", "cra-template-typescript");

        // Act
        string result = ReactAppTemplateMapper.ToLocal(contract);

        // Assert
        Assert.Equal("TypeScript", contract.Name);
        Assert.Equal("cra-template-typescript", result);
    }

    //the template is required: the server refuses an empty one, so normalization keeps it
    [Fact]
    public void Normalize_WhenTemplateIsEmpty_KeepsIt()
    {
        // Arrange
        StsReactAppTemplateDataModel contract = ReactAppTemplateMapper.ToContract("TypeScript", "");

        // Act
        StsReactAppTemplateDataModel result = ReactAppTemplateMapper.Normalize(contract);

        // Assert
        Assert.Equal(string.Empty, result.Template);
    }
}
