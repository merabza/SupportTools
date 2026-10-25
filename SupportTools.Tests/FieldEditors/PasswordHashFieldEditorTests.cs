using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.FieldEditors;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class PasswordHashFieldEditorTests : IDisposable
{
    private const string OldHash = "old hash";

    private readonly Queue<string?> _answers = new();
    private readonly List<string> _confirmQuestions = [];
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly List<string> _prompts = [];
    private bool _confirmAnswer;

    public PasswordHashFieldEditorTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    private PasswordHashFieldEditor CreateSut()
    {
        return new PasswordHashFieldEditor(nameof(ServerInstallationModel.PasswordHash), x => $"hash of {x}",
            prompt =>
            {
                _prompts.Add(prompt);
                return _answers.Dequeue();
            }, question =>
            {
                _confirmQuestions.Add(question);
                return _confirmAnswer;
            });
    }

    //the password is set while the installation is created, so a new installation is never left without it
    [Fact]
    public void Constructor_WhenCalled_IsAskedOnCreate()
    {
        // Act
        var sut = new PasswordHashFieldEditor(nameof(ServerInstallationModel.PasswordHash), x => x);

        // Assert
        Assert.True(sut.EnterFieldDataOnCreate);
    }

    [Fact]
    public async Task UpdateField_WithTheSamePasswordTwice_StoresOnlyItsHash()
    {
        // Arrange
        var installation = new ServerInstallationModel();
        _answers.Enqueue("secret");
        _answers.Enqueue("secret");

        // Act
        await CreateSut().UpdateField(null, installation);

        // Assert
        Assert.Equal("hash of secret", installation.PasswordHash);
        Assert.Equal(["Password Hash (password)", "Repeat password"], _prompts);
    }

    [Fact]
    public async Task UpdateField_WhenAHashIsSet_TellsThatAnEmptyPasswordKeepsIt()
    {
        // Arrange
        var installation = new ServerInstallationModel { PasswordHash = OldHash };
        _answers.Enqueue("secret");
        _answers.Enqueue("secret");

        // Act
        await CreateSut().UpdateField(null, installation);

        // Assert
        Assert.Equal("Password Hash (password, empty = keep)", _prompts[0]);
        Assert.Equal("hash of secret", installation.PasswordHash);
    }

    [Fact]
    public async Task UpdateField_WithADifferentRepeat_KeepsTheHashAndWritesAnError()
    {
        // Arrange
        var installation = new ServerInstallationModel { PasswordHash = OldHash };
        _answers.Enqueue("secret");
        _answers.Enqueue("other");

        // Act
        await CreateSut().UpdateField(null, installation);

        // Assert
        Assert.Equal(OldHash, installation.PasswordHash);
        Assert.Contains("[ERROR] Passwords do not match, nothing changed", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateField_WithEmptyPasswordAndConfirmedRemoval_RemovesTheHash()
    {
        // Arrange
        var installation = new ServerInstallationModel { PasswordHash = OldHash };
        _answers.Enqueue(string.Empty);
        _confirmAnswer = true;

        // Act
        await CreateSut().UpdateField(null, installation);

        // Assert
        Assert.Null(installation.PasswordHash);
        Assert.Equal(["Remove Password Hash?"], _confirmQuestions);
        Assert.Single(_prompts);
    }

    [Fact]
    public async Task UpdateField_WithEmptyPasswordAndRefusedRemoval_KeepsTheHash()
    {
        // Arrange
        var installation = new ServerInstallationModel { PasswordHash = OldHash };
        _answers.Enqueue(string.Empty);

        // Act
        await CreateSut().UpdateField(null, installation);

        // Assert
        Assert.Equal(OldHash, installation.PasswordHash);
        Assert.Single(_confirmQuestions);
    }

    //without a hash there is nothing to keep or remove
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateField_WithoutPasswordWhenNoHashIsSet_AsksNothingElse(string? answer)
    {
        // Arrange
        var installation = new ServerInstallationModel();
        _answers.Enqueue(answer);

        // Act
        await CreateSut().UpdateField(null, installation);

        // Assert
        Assert.Null(installation.PasswordHash);
        Assert.Single(_prompts);
        Assert.Empty(_confirmQuestions);
    }

    //the hash itself is never shown
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(OldHash, "set")]
    public void GetValueStatus_ForTheHash_ShowsOnlyWhetherItIsSet(string? hash, string expected)
    {
        // Arrange
        var installation = new ServerInstallationModel { PasswordHash = hash };

        // Act
        string result = CreateSut().GetValueStatus(installation);

        // Assert
        Assert.Equal(expected, result);
    }

    //the test host has no console input, so reading the answer fails right after the prompt is printed
    [Fact]
    public void InputPassword_OnTheConsole_AsksWithoutADefault()
    {
        // Act
        Assert.Throws<InvalidOperationException>(() => PasswordHashFieldEditor.InputPassword("Password Hash"));

        // Assert
        Assert.Contains("Enter Password Hash : ", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmRemove_OnTheConsole_AsksWithNoAsTheDefaultAnswer()
    {
        // Act
        Assert.Throws<InvalidOperationException>(() => PasswordHashFieldEditor.ConfirmRemove("Remove Password Hash?"));

        // Assert
        Assert.Contains("Remove Password Hash? (y/n)[n]: ", _consoleOutput.ToString(), StringComparison.Ordinal);
    }
}
