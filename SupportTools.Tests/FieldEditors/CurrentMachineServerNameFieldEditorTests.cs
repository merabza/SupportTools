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
public sealed class CurrentMachineServerNameFieldEditorTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

    private readonly SupportToolsParameters _parameters = new()
    {
        CurrentMachineServerName = "PAZISI",
        Servers =
        {
            ["PAZISI"] = new ServerDataModel { IsLocal = true },
            ["Merinson"] = new ServerDataModel { IsLocal = true },
            ["dl360"] = new ServerDataModel()
        }
    };

    private readonly List<(string FieldName, List<string> ServerNames, string? CurrentName)> _selections = [];
    private string? _selectedServerName;

    public CurrentMachineServerNameFieldEditorTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    //the public constructor selects on the console: the test host has no real console, so the selection throws after
    //the prompt with the current name is printed, and nothing is changed
    [Fact]
    public async Task PublicConstructor_WhenUpdatingField_PromptsWithCurrentServerName()
    {
        // Arrange
        var sut = new CurrentMachineServerNameFieldEditor(nameof(SupportToolsParameters.CurrentMachineServerName));

        // Act
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await sut.UpdateField(null, _parameters, CancellationToken.None));

        // Assert
        Assert.Contains("Select for Current Machine Server Name [PAZISI]: ", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.Equal("PAZISI", _parameters.CurrentMachineServerName);
        Assert.True(_parameters.Servers["Merinson"].IsLocal);
    }

    [Fact]
    public void PublicConstructor_WhenCreated_EditsGivenProperty()
    {
        // Act
        var sut = new CurrentMachineServerNameFieldEditor(nameof(SupportToolsParameters.CurrentMachineServerName));

        // Assert
        Assert.Equal(nameof(SupportToolsParameters.CurrentMachineServerName), sut.PropertyName);
    }

    //only existing servers can be selected: there is no "New" item
    [Fact]
    public async Task UpdateField_WhenSelecting_OffersServerNamesInOrderAndCurrentName()
    {
        // Arrange
        string[] expected = ["dl360", "Merinson", "PAZISI"];
        _selectedServerName = "PAZISI";

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        (string fieldName, List<string> serverNames, string? currentName) = Assert.Single(_selections);
        Assert.Equal("Current Machine Server Name", fieldName);
        Assert.Equal(expected, serverNames);
        Assert.Equal("PAZISI", currentName);
    }

    [Fact]
    public async Task UpdateField_WhenServerSelected_StoresItAndMarksOnlyItAsLocal()
    {
        // Arrange
        _selectedServerName = "Merinson";

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        Assert.Equal("Merinson", _parameters.CurrentMachineServerName);
        Assert.True(_parameters.Servers["Merinson"].IsLocal);
        Assert.False(_parameters.Servers["PAZISI"].IsLocal);
        Assert.False(_parameters.Servers["dl360"].IsLocal);
    }

    //(None): this computer is not in Servers, and the stored IsLocal values are kept as they are
    [Fact]
    public async Task UpdateField_WhenNoneSelected_ClearsNameAndKeepsIsLocalValues()
    {
        // Arrange
        _selectedServerName = null;

        // Act
        await CreateSut().UpdateField(null, _parameters, CancellationToken.None);

        // Assert
        Assert.Null(_parameters.CurrentMachineServerName);
        Assert.True(_parameters.Servers["PAZISI"].IsLocal);
        Assert.True(_parameters.Servers["Merinson"].IsLocal);
    }

    private CurrentMachineServerNameFieldEditor CreateSut()
    {
        return new CurrentMachineServerNameFieldEditor(nameof(SupportToolsParameters.CurrentMachineServerName),
            SelectServerName);
    }

    private string? SelectServerName(string fieldName, List<string> serverNames, string? currentName)
    {
        _selections.Add((fieldName, serverNames, currentName));
        return _selectedServerName;
    }
}
