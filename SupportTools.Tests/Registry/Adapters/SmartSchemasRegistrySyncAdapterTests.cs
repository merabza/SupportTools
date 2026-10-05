using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SmartSchemasRegistrySyncAdapterTests : IDisposable
{
    private const string Area = "smartschemas";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly SmartSchemasRegistrySyncAdapter _sut;

    public SmartSchemasRegistrySyncAdapterTests()
    {
        _context.Parameters.SmartSchemas["Daily"] = new SmartSchema
        {
            LastPreserveCount = 2, Details = [new SmartSchemaDetail { PeriodType = EPeriodType.Day, PreserveCount = 7 }]
        };
        _sut = new SmartSchemasRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    //a period type of a newer SupportTools cannot be stored here, and uploading the local record would lose it: the
    //record is left out of the sync on both sides until SupportTools is updated
    [Fact]
    public async Task GetServerRecords_WhenPeriodTypeIsUnknown_LeavesTheRecordOutOnBothSidesWithWarning()
    {
        // Arrange
        _context.Server.Store(Area, "Daily", NewContract("Daily", "Fortnight"), 3);
        _context.Server.Store(Area, "Hourly", NewContract("Hourly", "Hour"), 1);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords =
            await _sut.GetServerRecords(default);
        IReadOnlyDictionary<string, object> localRecords = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(["Hourly"], serverRecords.Value.Keys);
        Assert.Empty(localRecords);
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal(RegistryCollections.SmartSchemas, warning.CollectionName);
        Assert.Equal("Daily", warning.Key);
        Assert.Contains("Details.PeriodType", warning.Message, StringComparison.Ordinal);
        Assert.Contains("not synced until SupportTools is updated", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLocalRecords_WhenServerRecordBecameKnown_IncludesTheRecordAgain()
    {
        // Arrange
        _context.Server.Store(Area, "Daily", NewContract("Daily", "Fortnight"), 3);
        await _sut.GetServerRecords(default);
        _context.Server.Store(Area, "Daily", NewContract("Daily", "Day"), 4);
        await _sut.GetServerRecords(default);

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(["Daily"], result.Keys);
    }

    //the properties of SmartSchema are init-only, so the record is replaced
    [Fact]
    public void ApplyLocal_WhenRecordExists_ReplacesIt()
    {
        // Act
        _sut.ApplyLocal("Daily", NewContract("Daily", "month"));

        // Assert
        SmartSchemaDetail detail = Assert.Single(_context.Parameters.SmartSchemas["Daily"].Details);
        Assert.Equal(EPeriodType.Month, detail.PeriodType);
    }

    private static StsSmartSchemaDataModel NewContract(string name, string periodType)
    {
        return new StsSmartSchemaDataModel
        {
            Name = name,
            LastPreserveCount = 1,
            Details = [new StsSmartSchemaDetailDataModel { PeriodType = periodType, PreserveCount = 2 }]
        };
    }
}
