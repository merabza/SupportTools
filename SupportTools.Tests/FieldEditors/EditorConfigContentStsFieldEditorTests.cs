using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.LibDataInput;
using LibSupportToolsServerWork.FieldEditors;
using Xunit;

namespace SupportTools.Tests.FieldEditors;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class EditorConfigContentStsFieldEditorTests : IDisposable
{
    private readonly EditorConfigTestEnvironment _env = new();
    private readonly List<(string FieldName, string? DefaultValue)> _prompts = [];
    private string? _enteredFileName;

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public void PublicConstructor_WhenCreated_EditsGivenProperty()
    {
        // Act
        var sut = new EditorConfigContentStsFieldEditor(nameof(TextItemData.Text), _env.ParametersManager.Object, true);

        // Assert
        Assert.Equal(nameof(TextItemData.Text), sut.PropertyName);
        Assert.True(sut.EnterFieldDataOnCreate);
    }

    [Fact]
    public async Task UpdateField_WhenFileExists_LoadsItsContentIntoTheRecord()
    {
        // Arrange
        _enteredFileName = _env.TemplateFileName;
        var record = new TextItemData { Text = "old" };

        // Act
        await CreateSut().UpdateField("default", record, CancellationToken.None);

        // Assert
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent, record.Text);
    }

    [Fact]
    public async Task UpdateField_ForAnExistingRecord_OffersTheTemplateOfTheSameName()
    {
        // Arrange
        _enteredFileName = _env.TemplateFileName;

        // Act
        await CreateSut().UpdateField("BaGetter", new TextItemData(), CancellationToken.None);

        // Assert
        (string fieldName, string? defaultValue) = Assert.Single(_prompts);
        Assert.Equal("Content Source File", fieldName);
        Assert.Equal(Path.Combine(_env.TemplatesFolder, "BaGetter.editorconfig"), defaultValue);
    }

    //a new record's name is not passed to the field, so only the folder is offered
    [Fact]
    public async Task UpdateField_ForANewRecord_OffersTheTemplatesFolder()
    {
        // Arrange
        _enteredFileName = _env.TemplateFileName;

        // Act
        await CreateSut().UpdateField(null, new TextItemData(), CancellationToken.None);

        // Assert
        Assert.Equal(_env.TemplatesFolder + Path.DirectorySeparatorChar, Assert.Single(_prompts).DefaultValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task UpdateField_WhenTemplatesFolderIsNotSpecified_OffersNoFile(string? folderForEditorConfigFiles)
    {
        // Arrange
        _env.Parameters.FolderForEditorConfigFiles = folderForEditorConfigFiles;
        _enteredFileName = _env.TemplateFileName;

        // Act
        await CreateSut().UpdateField("default", new TextItemData(), CancellationToken.None);

        // Assert
        Assert.Null(Assert.Single(_prompts).DefaultValue);
    }

    //the record keeps its content, so nothing is uploaded in place of the missing file
    [Theory]
    [InlineData("missing.editorconfig")]
    [InlineData("")]
    public async Task UpdateField_WhenFileDoesNotExist_ThrowsAndKeepsTheContent(string fileName)
    {
        // Arrange
        _enteredFileName = fileName.Length == 0 ? fileName : Path.Combine(_env.TemplatesFolder, fileName);
        var record = new TextItemData { Text = "old" };

        // Act
        var exception = await Assert.ThrowsAsync<DataInputException>(async () =>
            await CreateSut().UpdateField("default", record, CancellationToken.None));

        // Assert
        Assert.Equal($"File {_enteredFileName} does not exist", exception.Message);
        Assert.Equal("old", record.Text);
    }

    [Fact]
    public void GetValueStatus_WhenContentIsLoaded_ShowsItsLength()
    {
        // Act
        string status = CreateSut().GetValueStatus(new TextItemData { Text = "root = true\n" });

        // Assert
        Assert.Equal("12 characters", status);
    }

    [Fact]
    public void GetValueStatus_WhenThereIsNoContent_IsEmpty()
    {
        // Act
        string status = CreateSut().GetValueStatus(new TextItemData());

        // Assert
        Assert.Equal(string.Empty, status);
    }

    private EditorConfigContentStsFieldEditor CreateSut()
    {
        return new EditorConfigContentStsFieldEditor(nameof(TextItemData.Text), _env.ParametersManager.Object, true,
            InputFilePath);
    }

    private string? InputFilePath(string fieldName, string? defaultValue)
    {
        _prompts.Add((fieldName, defaultValue));
        return _enteredFileName;
    }
}
