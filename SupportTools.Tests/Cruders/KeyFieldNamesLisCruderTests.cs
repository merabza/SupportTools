using System.Collections.Generic;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class KeyFieldNamesLisCruderTests
{
    private readonly List<string> _keyFieldNames = ["Code"];
    private readonly Mock<IParametersManager> _parametersManager = new();

    [Fact]
    public void Constructor_WhenCalled_NamesTheKeyFieldNameRecords()
    {
        // Act
        KeyFieldNamesLisCruder sut = CreateSut();

        // Assert
        Assert.Equal("Key Field Name", sut.CrudName);
        Assert.Equal("Key Field Names", sut.CrudNamePlural);
    }

    [Fact]
    public async Task AddRecordWithKey_WhenCalled_AddsTheNameToTheGivenList()
    {
        // Arrange
        KeyFieldNamesLisCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "Year", new TextItemData());

        // Assert
        Assert.Equal(["Code", "Year"], _keyFieldNames);
        Assert.True(sut.ContainsRecordWithKey("Year"));
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenCalled_RemovesTheNameFromTheGivenList()
    {
        // Arrange
        KeyFieldNamesLisCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "Code");

        // Assert
        Assert.Empty(_keyFieldNames);
        Assert.False(sut.ContainsRecordWithKey("Code"));
    }

    private KeyFieldNamesLisCruder CreateSut()
    {
        return new KeyFieldNamesLisCruder(_parametersManager.Object, _keyFieldNames);
    }
}
