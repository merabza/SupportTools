using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.FieldEditors;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class EditorConfigPatternNameFieldEditorTests : IDisposable
{
    private readonly EditorConfigTestEnvironment _env = new();
    private readonly List<(string FieldName, string? CurrentName)> _selections = [];
    private string? _selectedPatternName;

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public void PublicConstructor_WhenCreated_EditsGivenProperty()
    {
        // Act
        var sut = new EditorConfigPatternNameFieldEditor(new Mock<ILogger>().Object,
            nameof(ProjectModel.EditorConfigPatternName), _env.ParametersManager.Object, true);

        // Assert
        Assert.Equal(nameof(ProjectModel.EditorConfigPatternName), sut.PropertyName);
        Assert.True(sut.EnterFieldDataOnCreate);
    }

    [Fact]
    public async Task UpdateField_WhenSelecting_OffersFieldNameAndCurrentPatternName()
    {
        // Arrange
        ProjectModel project = _env.Parameters.Projects[EditorConfigTestEnvironment.ProjectName];

        // Act
        await CreateSut().UpdateField(EditorConfigTestEnvironment.ProjectName, project, CancellationToken.None);

        // Assert
        (string fieldName, string? currentName) = Assert.Single(_selections);
        Assert.Equal("Editor Config Pattern Name", fieldName);
        Assert.Equal(EditorConfigTestEnvironment.PatternName, currentName);
    }

    [Fact]
    public async Task UpdateField_WhenPatternSelected_StoresItInRecord()
    {
        // Arrange
        _selectedPatternName = "React";
        ProjectModel project = _env.Parameters.Projects[EditorConfigTestEnvironment.ProjectName];

        // Act
        await CreateSut().UpdateField(EditorConfigTestEnvironment.ProjectName, project, CancellationToken.None);

        // Assert
        Assert.Equal("React", project.EditorConfigPatternName);
    }

    //the pattern is optional: selecting (None) clears it
    [Fact]
    public async Task UpdateField_WhenNoneSelected_ClearsPatternNameOfRecord()
    {
        // Arrange
        _selectedPatternName = null;
        ProjectModel project = _env.Parameters.Projects[EditorConfigTestEnvironment.ProjectName];

        // Act
        await CreateSut().UpdateField(EditorConfigTestEnvironment.ProjectName, project, CancellationToken.None);

        // Assert
        Assert.Null(project.EditorConfigPatternName);
    }

    [Fact]
    public void GetValueStatus_WhenPatternNameIsNotSet_ReturnsEmpty()
    {
        // Arrange
        var project = new ProjectModel();

        // Act
        string status = CreateSut().GetValueStatus(project);

        // Assert
        Assert.Equal(string.Empty, status);
    }

    [Fact]
    public void GetValueStatus_WhenPatternNameIsSet_ReturnsNameWithUsageCount()
    {
        // Arrange
        ProjectModel project = _env.Parameters.Projects[EditorConfigTestEnvironment.ProjectName];

        // Act
        string status = CreateSut().GetValueStatus(project);

        // Assert
        Assert.Equal($"{EditorConfigTestEnvironment.PatternName} (Usage count is: 1)", status);
    }

    private EditorConfigPatternNameFieldEditor CreateSut()
    {
        return new EditorConfigPatternNameFieldEditor(new Mock<ILogger>().Object,
            nameof(ProjectModel.EditorConfigPatternName), _env.ParametersManager.Object, false, SelectPatternName);
    }

    private ValueTask<string?> SelectPatternName(string fieldName, string? currentName,
        CancellationToken cancellationToken)
    {
        _selections.Add((fieldName, currentName));
        return ValueTask.FromResult(_selectedPatternName);
    }
}
