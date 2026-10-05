using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class NpmPackageMapperTests
{
    //a scoped package name holds a slash
    [Theory]
    [InlineData("Testing library")]
    [InlineData("")]
    public void ToLocal_WhenContractComesFromLocalRecord_GivesTheOriginalDescription(string description)
    {
        // Arrange
        StsNpmPackageDataModel contract = NpmPackageMapper.ToContract("@testing-library/react", description);

        // Act
        string result = NpmPackageMapper.ToLocal(contract);

        // Assert
        Assert.Equal("@testing-library/react", contract.Name);
        Assert.Equal(description, result);
    }

    [Fact]
    public void ToLocal_WhenDescriptionIsNull_GivesEmptyString()
    {
        // Act
        string result = NpmPackageMapper.ToLocal(new StsNpmPackageDataModel { Name = "react" });

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WhenDescriptionIsEmptyOrNull_GivesTheSameHash()
    {
        // Act
        string emptyHash = MapperTestHelpers.HashOf(NpmPackageMapper.Normalize(NpmPackageMapper.ToContract("x", "")));
        string nullHash = MapperTestHelpers.HashOf(NpmPackageMapper.Normalize(NpmPackageMapper.ToContract("x", null)));

        // Assert
        Assert.Equal(emptyHash, nullHash);
    }
}
