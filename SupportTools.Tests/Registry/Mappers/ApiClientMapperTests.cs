using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibApiClientParameters;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ApiClientMapperTests
{
    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalRecord_RestoresTheOriginal()
    {
        // Arrange
        var original = new ApiClientSettings { Server = "https://agent.example.test/api", ApiKey = "fake-api-key" };
        StsApiClientDataModel contract = ApiClientMapper.ToContract("Pc1.WebAgent", original);
        var restored = new ApiClientSettings();

        // Act
        ApiClientMapper.ApplyToLocal(contract, restored);

        // Assert
        Assert.Equal("Pc1.WebAgent", contract.Name);
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    [Fact]
    public void Normalize_WhenTextsAreEmptyOrNull_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsApiClientDataModel { Name = "A", Server = "", ApiKey = "" };
        var missing = new StsApiClientDataModel { Name = "A" };

        // Act
        string emptyHash = MapperTestHelpers.HashOf(ApiClientMapper.Normalize(empty));
        string missingHash = MapperTestHelpers.HashOf(ApiClientMapper.Normalize(missing));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }
}
