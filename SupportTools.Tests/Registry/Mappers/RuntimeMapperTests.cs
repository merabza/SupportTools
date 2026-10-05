using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class RuntimeMapperTests
{
    [Theory]
    [InlineData("Windows x64")]
    [InlineData("")]
    public void ToLocal_WhenContractComesFromLocalRecord_GivesTheOriginalDescription(string description)
    {
        // Arrange
        StsRuntimeDataModel contract = RuntimeMapper.ToContract("win-x64", description);

        // Act
        string result = RuntimeMapper.ToLocal(contract);

        // Assert
        Assert.Equal("win-x64", contract.Name);
        Assert.Equal(description, result);
    }

    [Fact]
    public void ToLocal_WhenDescriptionIsNull_GivesEmptyString()
    {
        // Act
        string result = RuntimeMapper.ToLocal(new StsRuntimeDataModel { Name = "win-x64" });

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WhenDescriptionIsEmptyOrNull_GivesTheSameHash()
    {
        // Act
        string emptyHash = MapperTestHelpers.HashOf(RuntimeMapper.Normalize(RuntimeMapper.ToContract("x", "")));
        string nullHash = MapperTestHelpers.HashOf(RuntimeMapper.Normalize(RuntimeMapper.ToContract("x", null)));

        // Assert
        Assert.Equal(emptyHash, nullHash);
    }
}
