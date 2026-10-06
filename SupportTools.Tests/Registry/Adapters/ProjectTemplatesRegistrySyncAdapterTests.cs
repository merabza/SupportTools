using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectTemplatesRegistrySyncAdapterTests : IDisposable
{
    private const string Area = "projecttemplates";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly ProjectTemplatesRegistrySyncAdapter _sut;

    public ProjectTemplatesRegistrySyncAdapterTests()
    {
        _sut = new ProjectTemplatesRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void GetLocalRecords_WhenProjectCreatorParametersAreMissing_GivesNoRecordAndCreatesNothing()
    {
        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(result);
        Assert.Null(_context.Parameters.AppProjectCreatorAllParameters);
    }

    //a new computer: the templates live in AppProjectCreatorAllParameters, which does not exist yet
    [Fact]
    public void ApplyLocal_WhenProjectCreatorParametersAreMissing_CreatesThemWithTheTemplate()
    {
        // Act
        _sut.ApplyLocal("Console With Database", NewContract("Console With Database", "Console"));

        // Assert
        TemplateModel template = _context.Parameters.AppProjectCreatorAllParameters!.Templates["Console With Database"];
        Assert.Equal(ESupportProjectType.Console, template.SupportProjectType);
        Assert.True(template.UseDatabase);
        Assert.Equal(["Console With Database"], _sut.GetLocalRecords().Keys);
    }

    [Fact]
    public void ApplyLocal_WhenProjectCreatorParametersExist_KeepsThemAndTheirOtherTemplates()
    {
        // Arrange
        var existing = new AppProjectCreatorAllParameters
        {
            IndentSize = 4, Templates = { ["Console"] = new TemplateModel() }
        };
        _context.Parameters.AppProjectCreatorAllParameters = existing;

        // Act
        _sut.ApplyLocal("Api", NewContract("Api", "Api"));

        // Assert
        Assert.Same(existing, _context.Parameters.AppProjectCreatorAllParameters);
        Assert.Equal(4, existing.IndentSize);
        Assert.Equal(["Console", "Api"], existing.Templates.Keys);
    }

    [Fact]
    public void RemoveLocal_WhenCalled_RemovesTheTemplate()
    {
        // Arrange
        _sut.ApplyLocal("Console With Database", NewContract("Console With Database", "Console"));

        // Act
        _sut.RemoveLocal("Console With Database");

        // Assert
        Assert.Empty(_context.Parameters.AppProjectCreatorAllParameters!.Templates);
    }

    [Fact]
    public async Task GetServerRecords_WhenProjectTypeIsUnknown_LeavesTheRecordOutWithWarning()
    {
        // Arrange
        _context.Server.Store(Area, "Worker", NewContract("Worker", "Worker"), 1);
        _context.Server.Store(Area, "Api", NewContract("Api", "Api"), 1);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(["Api"], result.Value.Keys);
        Assert.Equal("Worker", Assert.Single(_context.Warnings.Items).Key);
    }

    private static StsProjectTemplateDataModel NewContract(string name, string supportProjectType)
    {
        return new StsProjectTemplateDataModel
        {
            Name = name, SupportProjectType = supportProjectType, UseDatabase = true
        };
    }
}
