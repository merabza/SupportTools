using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class EnvironmentMapperTests
{
    [Theory]
    [InlineData("Production environment")]
    [InlineData("")]
    public void ToLocal_WhenContractComesFromLocalRecord_GivesTheOriginalDescription(string description)
    {
        // Arrange
        StsEnvironmentDataModel contract = EnvironmentMapper.ToContract("Production", description);

        // Act
        string result = EnvironmentMapper.ToLocal(contract);

        // Assert
        Assert.Equal("Production", contract.Name);
        Assert.Equal(description, result);
    }

    //the server keeps no description as null, the local dictionary has no null values
    [Fact]
    public void ToLocal_WhenDescriptionIsNull_GivesEmptyString()
    {
        // Act
        string result = EnvironmentMapper.ToLocal(new StsEnvironmentDataModel { Name = "Production" });

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WhenDescriptionIsEmptyOrNull_GivesTheSameHash()
    {
        // Act
        string emptyHash =
            MapperTestHelpers.HashOf(EnvironmentMapper.Normalize(EnvironmentMapper.ToContract("P", "")));
        string nullHash =
            MapperTestHelpers.HashOf(EnvironmentMapper.Normalize(EnvironmentMapper.ToContract("P", null)));

        // Assert
        Assert.Equal(emptyHash, nullHash);
    }
}
