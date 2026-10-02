using System.Collections.Generic;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class NpmPackagesCruderTests
{
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public NpmPackagesCruderTests()
    {
        _parameters.NpmPackages["fake-ui-kit"] = "Fake UI components";
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheNpmPackageRecords()
    {
        // Act
        var sut = new NpmPackagesCruder(_parametersManager.Object, []);

        // Assert
        Assert.Equal("Npm Package", sut.CrudName);
        Assert.Equal("Npm Packages", sut.CrudNamePlural);
    }

    [Fact]
    public void Constructor_WhenCalled_EditsTheGivenDictionary()
    {
        // Arrange
        Dictionary<string, string> npmPackages = new() { ["fake-router"] = "Fake router" };

        // Act
        var sut = new NpmPackagesCruder(_parametersManager.Object, npmPackages);

        // Assert
        Assert.True(sut.ContainsRecordWithKey("fake-router"));
        Assert.False(sut.ContainsRecordWithKey("fake-ui-kit"));
        Assert.Equal("Fake router", sut.GetStatusFor("fake-router"));
    }

    [Fact]
    public async Task Create_WhenCalled_EditsTheNpmPackagesOfTheParameters()
    {
        // Arrange
        var sut = NpmPackagesCruder.Create(_parametersManager.Object);

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "fake-forms", new TextItemData { Text = "Fake forms" });

        // Assert
        Assert.True(sut.ContainsRecordWithKey("fake-ui-kit"));
        Assert.Equal("Fake forms", _parameters.NpmPackages["fake-forms"]);
    }
}
