using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the same base as the .gitignore templates: these tests check the .editorconfig files and endpoints
[Collection(ConsoleCaptureCollection.Name)]
public sealed class EditorConfigPatternsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = FakeSupportToolsServer.EditorConfigFileTypes;
    private const string Content = "root = true\r\n\r\n[*.cs]\r\n# ქართული\r\nindent_size = 4\r\n";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly string _folder;
    private readonly EditorConfigPatternsRegistrySyncAdapter _sut;

    public EditorConfigPatternsRegistrySyncAdapterTests()
    {
        _folder = Path.Combine(_context.TempFolder, "editorconfig");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SupportToolsParameters.GetEditorConfigPatternFilePath(_folder, "default"), Content);
        _context.Parameters.FolderForEditorConfigFiles = _folder;
        _context.Parameters.EditorConfigPatterns.Add("default");
        _sut = new EditorConfigPatternsRegistrySyncAdapter(_context.ApiClient, _context.Parameters,
            _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues()
    {
        // Assert
        Assert.Equal((RegistryCollections.EditorConfigPatterns, RegistryCollections.EditorConfigPatternsOrder),
            (_sut.CollectionName, _sut.Order));
    }

    [Fact]
    public async Task GetServerRecords_WhenServerHasTemplates_ReturnsThemWithTheirVersions()
    {
        // Arrange
        _context.Server.Store(Area, "strict", new StsEditorConfigFileTypeDataModel { Name = "strict", Content = "" },
            4);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(4, result.Value["strict"].Version);
    }

    [Fact]
    public void GetLocalRecords_WhenFolderIsNotSet_GivesNoRecordWithWarningNamingTheField()
    {
        // Arrange
        _context.Parameters.FolderForEditorConfigFiles = "";

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(result);
        Assert.Contains(nameof(SupportToolsParameters.FolderForEditorConfigFiles),
            Assert.Single(_context.Warnings.Items).Message, StringComparison.Ordinal);
    }

    //the user decided (C3) that the file stays on the disk
    [Fact]
    public void RemoveLocal_WhenCalled_RemovesTheNameAndKeepsTheFile()
    {
        // Act
        _sut.RemoveLocal("default");

        // Assert
        Assert.Empty(_context.Parameters.EditorConfigPatterns);
        Assert.True(File.Exists(SupportToolsParameters.GetEditorConfigPatternFilePath(_folder, "default")));
    }

    //the content belongs to a file and goes as it is
    [Fact]
    public void Normalize_WhenCalled_KeepsTheContent()
    {
        // Arrange
        var contract = new StsEditorConfigFileTypeDataModel { Name = "default", Content = Content };

        // Act
        object result = _sut.Normalize(contract);

        // Assert
        Assert.Equal(Content, Assert.IsType<StsEditorConfigFileTypeDataModel>(result).Content);
    }

    [Fact]
    public void GetLocalRecords_WhenFileExists_GivesNameAndFileContent()
    {
        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(Content, Assert.IsType<StsEditorConfigFileTypeDataModel>(result["default"]).Content);
    }

    [Fact]
    public void ApplyLocal_WhenTemplateIsNew_WritesTheFileAndAddsTheName()
    {
        // Act
        _sut.ApplyLocal("strict", new StsEditorConfigFileTypeDataModel { Name = "strict", Content = Content });

        // Assert
        Assert.Equal(Content,
            File.ReadAllText(SupportToolsParameters.GetEditorConfigPatternFilePath(_folder, "strict")));
        Assert.Equal(["default", "strict"], _context.Parameters.EditorConfigPatterns);
    }

    [Fact]
    public async Task Upsert_WhenVersionIsExpected_UploadsAndReturnsTheNewVersion()
    {
        // Arrange
        _context.Server.Store(Area, "default", new StsEditorConfigFileTypeDataModel { Name = "default", Content = "" },
            3);

        // Act
        Result<int> result = await _sut.Upsert("default", _sut.GetLocalRecords()["default"], 3, default);

        // Assert
        Assert.Equal(4, result.Value);
        Assert.Equal(Content, _context.Server.Get<StsEditorConfigFileTypeDataModel>(Area, "default")?.Content);
        Assert.Equal(1, _context.Server.RequestCount("POST", "/git/syncupeditorconfigfiletypes/True"));
    }

    //the old endpoint answers EditorConfigFileTypeWithNameNotFound
    [Fact]
    public async Task Delete_WhenRecordIsDeletedBetweenCheckAndDelete_ReturnsRecordWithNameNotFound()
    {
        // Arrange
        _context.Server.Store(Area, "default", new StsEditorConfigFileTypeDataModel { Name = "default", Content = "" },
            3);
        _context.Server.BeforeRequest = request =>
        {
            if (request.StartsWith("DELETE", StringComparison.Ordinal))
            {
                _context.Server.Records(Area).Clear();
            }
        };

        // Act
        Result result = await _sut.Delete("default", 3, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RecordWithNameNotFound, result.Error.Code);
        Assert.Equal(1, _context.Server.RequestCount("DELETE", "/git/deleteeditorconfigfiletype/default"));
    }
}
