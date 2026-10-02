using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectNpmPackagesLisCruderTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<string> _inputFieldNames = [];
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<string> _projectNpmPackages = ["fake-ui-kit"];

    public ProjectNpmPackagesLisCruderTests()
    {
        _parameters.NpmPackages["fake-ui-kit"] = "Fake UI components";
        _parameters.NpmPackages["fake-router"] = "Fake router";
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheNpmPackageRecords()
    {
        // Act
        var sut = new ProjectNpmPackagesLisCruder(new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object, _projectNpmPackages);

        // Assert
        Assert.Equal("Npm Package", sut.CrudName);
        Assert.Equal("Npm Packages", sut.CrudNamePlural);
        Assert.True(sut.ContainsRecordWithKey("fake-ui-kit"));
    }

    [Fact]
    public async Task AddRecordWithKey_WhenCalled_AddsThePackageToTheProjectList()
    {
        // Arrange
        ProjectNpmPackagesLisCruder sut = CreateSut(null);

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "fake-router", new TextItemData());

        // Assert
        Assert.Equal(["fake-ui-kit", "fake-router"], _projectNpmPackages);
    }

    //the name of a new record is chosen (or created) among the registered npm packages
    [Fact]
    public async Task InputNewRecordName_WhenPackageIsChosen_ReturnsItsName()
    {
        // Arrange
        ProjectNpmPackagesLisCruder sut = CreateSut("fake-router");

        // Act
        string? result = await CliMenuTestAccess.InvokeInputNewRecordName(sut);

        // Assert
        Assert.Equal("fake-router", result);
        Assert.Equal(["Npm Package Name"], _inputFieldNames);
        Assert.DoesNotContain("Name is empty", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InputNewRecordName_WhenNameIsEmpty_ReportsItAndReturnsNull(string? enteredName)
    {
        // Arrange
        ProjectNpmPackagesLisCruder sut = CreateSut(enteredName);

        // Act
        string? result = await CliMenuTestAccess.InvokeInputNewRecordName(sut);

        // Assert
        Assert.Null(result);
        Assert.Contains("[ERROR] Name is empty", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the status of a project package is the description of the registered npm package
    [Fact]
    public void GetStatusFor_WhenPackageIsRegistered_ReturnsItsDescription()
    {
        // Act
        string? status = CreateSut(null).GetStatusFor("fake-ui-kit");

        // Assert
        Assert.Equal("Fake UI components", status);
    }

    [Fact]
    public void GetStatusFor_WhenPackageIsNotRegistered_ReturnsNoStatus()
    {
        // Act
        string? status = CreateSut(null).GetStatusFor("missing-package");

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsThePackagesAndOffersToReCreateTheFrontProject()
    {
        // Act
        CliMenuSet listMenu = CreateSut(null).GetListMenu();

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand)];
        Assert.Equal(["fake-ui-kit"], commands.OfType<ItemSubMenuCliMenuCommand>().Select(x => x.Name));
        Assert.Single(commands.OfType<ReCreateUpdateFrontSpaProjectCliMenuCommand>());
    }

    private ProjectNpmPackagesLisCruder CreateSut(string? enteredName)
    {
        return new ProjectNpmPackagesLisCruder(new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object, _projectNpmPackages, (fieldName, _) =>
            {
                _inputFieldNames.Add(fieldName);
                return ValueTask.FromResult(enteredName);
            });
    }
}
