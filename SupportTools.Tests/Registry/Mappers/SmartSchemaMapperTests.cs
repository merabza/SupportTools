using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class SmartSchemaMapperTests
{
    [Fact]
    public void ToLocal_WhenContractComesFromLocalRecord_RestoresTheOriginal()
    {
        // Arrange
        SmartSchema original = NewSmartSchema();
        StsSmartSchemaDataModel contract = SmartSchemaMapper.ToContract("Daily", original);

        // Act
        SmartSchema result = SmartSchemaMapper.ToLocal(contract);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(result));
    }

    [Fact]
    public void ToContract_WhenCalled_WritesPeriodTypesAsNames()
    {
        // Act
        StsSmartSchemaDataModel result = SmartSchemaMapper.ToContract("Daily", NewSmartSchema());

        // Assert
        Assert.Equal(["Month", "Day"], result.Details.ConvertAll(x => x.PeriodType));
    }

    //the server returns the details ordered by type; their order means nothing
    [Fact]
    public void Normalize_WhenDetailsAreInAnotherOrder_GivesTheSameHash()
    {
        // Arrange
        StsSmartSchemaDataModel local = SmartSchemaMapper.ToContract("Daily", NewSmartSchema());
        var server = new StsSmartSchemaDataModel
        {
            Name = "Daily",
            LastPreserveCount = 3,
            Details =
            [
                new StsSmartSchemaDetailDataModel { PeriodType = "day", PreserveCount = 7 },
                new StsSmartSchemaDetailDataModel { PeriodType = "Month", PreserveCount = 12 }
            ],
            Version = 4
        };

        // Act
        string localHash = MapperTestHelpers.HashOf(SmartSchemaMapper.Normalize(local));
        string serverHash = MapperTestHelpers.HashOf(SmartSchemaMapper.Normalize(server));

        // Assert
        Assert.Equal(localHash, serverHash);
    }

    [Theory]
    [InlineData("Day", false)]
    [InlineData("Fortnight", true)]
    public void HasUnknownPeriodType_WhenCalled_FindsTypesUnknownToThisClient(string periodType, bool expected)
    {
        // Arrange
        var contract = new StsSmartSchemaDataModel
        {
            Name = "Daily",
            Details =
            [
                new StsSmartSchemaDetailDataModel { PeriodType = "Month", PreserveCount = 1 },
                new StsSmartSchemaDetailDataModel { PeriodType = periodType, PreserveCount = 1 }
            ]
        };

        // Act
        bool result = SmartSchemaMapper.HasUnknownPeriodType(contract);

        // Assert
        Assert.Equal(expected, result);
    }

    private static SmartSchema NewSmartSchema()
    {
        return new SmartSchema
        {
            LastPreserveCount = 3,
            Details =
            [
                new SmartSchemaDetail { PeriodType = EPeriodType.Month, PreserveCount = 12 },
                new SmartSchemaDetail { PeriodType = EPeriodType.Day, PreserveCount = 7 }
            ]
        };
    }
}
