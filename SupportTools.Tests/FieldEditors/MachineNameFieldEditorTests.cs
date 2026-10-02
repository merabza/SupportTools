using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.FieldEditors;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class MachineNameFieldEditorTests : IDisposable
{
    private const string ThisMachineName = "THIS-PC";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly List<(string FieldName, string? DefaultValue)> _prompts = [];
    private string? _answer;

    public MachineNameFieldEditorTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    //the public constructor asks on the console: the test host has no console input, so reading the answer throws
    //after the prompt with the name of this computer is printed
    [Fact]
    public async Task PublicConstructor_WhenUpdatingField_PromptsWithNameOfThisComputer()
    {
        // Arrange
        var sut = new MachineNameFieldEditor(nameof(SupportToolsParameters.MachineName));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.UpdateField(null, _parameters, CancellationToken.None));

        // Assert
        Assert.Contains($"Enter Machine Name [{Environment.MachineName}]: ", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.Null(_parameters.MachineName);
    }

    [Fact]
    public void PublicConstructor_WhenMachineNameIsNotSet_ShowsNameOfThisComputer()
    {
        // Arrange
        var sut = new MachineNameFieldEditor(nameof(SupportToolsParameters.MachineName));

        // Act
        string status = sut.GetValueStatus(_parameters);

        // Assert
        Assert.Equal(nameof(SupportToolsParameters.MachineName), sut.PropertyName);
        Assert.Equal(Environment.MachineName, status);
    }

    //like a plain text field, the name is asked when a record with it is created
    [Fact]
    public void PublicConstructor_WhenCreated_AsksFieldOnCreate()
    {
        // Act
        var sut = new MachineNameFieldEditor(nameof(SupportToolsParameters.MachineName));

        // Assert
        Assert.True(sut.EnterFieldDataOnCreate);
    }

    [Fact]
    public void GetValueStatus_WhenMachineNameIsSet_ShowsIt()
    {
        // Arrange
        _parameters.MachineName = "Merinson";

        // Act
        string status = CreateSut().GetValueStatus(_parameters);

        // Assert
        Assert.Equal("Merinson", status);
    }

    [Fact]
    public async Task UpdateField_WhenMachineNameIsNotSet_OffersNameOfThisComputer()
    {
        // Arrange
        _answer = ThisMachineName;

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        (string fieldName, string? defaultValue) = Assert.Single(_prompts);
        Assert.Equal("Machine Name", fieldName);
        Assert.Equal(ThisMachineName, defaultValue);
    }

    [Fact]
    public async Task UpdateField_WhenMachineNameIsSet_OffersIt()
    {
        // Arrange
        _parameters.MachineName = "Merinson";
        _answer = "Merinson";

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        Assert.Equal("Merinson", Assert.Single(_prompts).DefaultValue);
    }

    //a copied file must give the right name on the other computer, so the offered name is not stored
    [Theory]
    [InlineData(ThisMachineName)]
    [InlineData("this-pc")]
    [InlineData("  THIS-PC  ")]
    public async Task UpdateField_WhenNameOfThisComputerEntered_LeavesFieldEmpty(string answer)
    {
        // Arrange
        _answer = answer;

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        Assert.Null(_parameters.MachineName);
    }

    [Fact]
    public async Task UpdateField_WhenStoredNameIsReplacedWithNameOfThisComputer_ClearsIt()
    {
        // Arrange
        _parameters.MachineName = "Merinson";
        _answer = ThisMachineName;

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        Assert.Null(_parameters.MachineName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateField_WhenNothingEntered_LeavesFieldEmpty(string? answer)
    {
        // Arrange
        _parameters.MachineName = "Merinson";
        _answer = answer;

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        Assert.Null(_parameters.MachineName);
    }

    [Fact]
    public async Task UpdateField_WhenOtherNameEntered_StoresItTrimmed()
    {
        // Arrange
        _answer = "  Merinson  ";

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        Assert.Equal("Merinson", _parameters.MachineName);
    }

    private MachineNameFieldEditor CreateSut()
    {
        return new MachineNameFieldEditor(nameof(SupportToolsParameters.MachineName), ThisMachineName, InputText);
    }

    private string? InputText(string fieldName, string? defaultValue)
    {
        _prompts.Add((fieldName, defaultValue));
        return _answer;
    }
}
