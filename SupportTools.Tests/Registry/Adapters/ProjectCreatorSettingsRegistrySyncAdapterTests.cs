using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectCreatorSettingsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = FakeSupportToolsServer.ProjectCreatorSettings;
    private const string Key = ProjectCreatorSettingsRegistrySyncAdapter.RecordKey;
    private readonly RegistryAdapterTestContext _context = new();
    private readonly ProjectCreatorSettingsRegistrySyncAdapter _sut;

    public ProjectCreatorSettingsRegistrySyncAdapterTests()
    {
        _context.PathMapper = MapperTestHelpers.LinuxPathMapper();
        _sut = new ProjectCreatorSettingsRegistrySyncAdapter(_context.ApiClient, _context.Parameters,
            _context.PathMapper, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues()
    {
        // Assert
        Assert.Equal((RegistryCollections.ProjectCreatorSettings, RegistryCollections.ProjectCreatorSettingsOrder),
            (_sut.CollectionName, _sut.Order));
    }

    [Fact]
    public async Task GetServerRecords_WhenServerRecordHasData_GivesItWithItsVersion()
    {
        // Arrange
        _context.Server.Store(Area, string.Empty, new StsProjectCreatorSettingsDataModel { IndentSize = 4 }, 2);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(2, result.Value[Key].Version);
    }

    [Fact]
    public async Task GetServerRecords_WhenServerHasNoRecord_GivesNoRecord()
    {
        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Upsert_WhenServerHasNoRecord_CreatesIt()
    {
        // Arrange
        await _sut.GetServerRecords(default);

        // Act
        Result<int> result = await _sut.Upsert(Key, new StsProjectCreatorSettingsDataModel { IndentSize = 4 }, 0,
            default);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.Equal(4, _context.Server.Get<StsProjectCreatorSettingsDataModel>(Area, string.Empty)?.IndentSize);
    }

    //the server deletes no singleton: deleting means writing an empty record
    [Fact]
    public async Task Delete_WhenVersionIsExpected_WritesAnEmptyRecord()
    {
        // Arrange
        _context.Server.Store(Area, string.Empty, new StsProjectCreatorSettingsDataModel { IndentSize = 4 }, 2);

        // Act
        Result result = await _sut.Delete(Key, 2, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, _context.Server.Get<StsProjectCreatorSettingsDataModel>(Area, string.Empty)?.IndentSize);
    }

    [Fact]
    public void ApplyLocal_WhenProjectCreatorParametersExist_UpdatesThemInPlace()
    {
        // Arrange
        var existing = new AppProjectCreatorAllParameters { Templates = { ["Console"] = new TemplateModel() } };
        _context.Parameters.AppProjectCreatorAllParameters = existing;

        // Act
        _sut.ApplyLocal(Key, new StsProjectCreatorSettingsDataModel { IndentSize = 2 });

        // Assert
        Assert.Same(existing, _context.Parameters.AppProjectCreatorAllParameters);
        Assert.Equal(2, existing.IndentSize);
        Assert.Equal(["Console"], existing.Templates.Keys);
    }

    [Fact]
    public void GetLocalRecords_WhenProjectCreatorParametersAreMissing_GivesNoRecord()
    {
        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(result);
    }

    //only templates: the shared part is empty
    [Fact]
    public void GetLocalRecords_WhenOnlyTemplatesExist_GivesNoRecord()
    {
        // Arrange
        _context.Parameters.AppProjectCreatorAllParameters =
            new AppProjectCreatorAllParameters { Templates = { ["Console"] = new TemplateModel() } };

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetLocalRecords_WhenComputerIsLinux_GivesCanonicalPaths()
    {
        // Arrange
        _context.Parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            IndentSize = 4, ProjectsFolderPathReal = "/home/u/1WorkDotnet/Projects"
        };

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal("ProjectCreator", Key);
        Assert.Equal(@"D:\1WorkDotnet\Projects",
            Assert.IsType<StsProjectCreatorSettingsDataModel>(result[Key]).ProjectsFolderPathReal);
    }

    [Fact]
    public void ApplyLocal_WhenProjectCreatorParametersAreMissing_CreatesThemWithLocalPaths()
    {
        // Act
        _sut.ApplyLocal(Key,
            new StsProjectCreatorSettingsDataModel
            {
                IndentSize = 4, SecretsFolderPathReal = @"D:\1WorkDotnet\Secrets"
            });

        // Assert
        AppProjectCreatorAllParameters result = _context.Parameters.AppProjectCreatorAllParameters!;
        Assert.Equal(4, result.IndentSize);
        Assert.Equal("/home/u/1WorkDotnet/Secrets", result.SecretsFolderPathReal);
    }

    [Fact]
    public void RemoveLocal_WhenCalled_ClearsTheSharedPartAndKeepsTemplates()
    {
        // Arrange
        _context.Parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            IndentSize = 4, FakeHostProjectName = "FakeHost", Templates = { ["Console"] = new TemplateModel() }
        };

        // Act
        _sut.RemoveLocal(Key);

        // Assert
        Assert.Empty(_sut.GetLocalRecords());
        Assert.Equal(["Console"], _context.Parameters.AppProjectCreatorAllParameters.Templates.Keys);
    }
}
