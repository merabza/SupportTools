using System;
using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Adapters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//IsLocal does not travel: after a pull it is computed again from CurrentMachineServerName (G6)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ServersRegistrySyncAdapterTests : IDisposable
{
    private readonly RegistryAdapterTestContext _context = new();
    private readonly ServersRegistrySyncAdapter _sut;

    public ServersRegistrySyncAdapterTests()
    {
        _context.Parameters.Servers["PAZISI"] = new ServerDataModel { IsLocal = true, Runtime = "win-x64" };
        _sut = new ServersRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void ApplyLocal_WhenPulledServerIsThisComputer_MarksOnlyItAsLocal()
    {
        // Arrange
        _context.Parameters.CurrentMachineServerName = "merinson";

        // Act
        _sut.ApplyLocal("Merinson", new StsServerDataModel { Name = "Merinson", Runtime = "linux-x64" });

        // Assert
        Assert.True(_context.Parameters.Servers["Merinson"].IsLocal);
        Assert.False(_context.Parameters.Servers["PAZISI"].IsLocal);
    }

    [Fact]
    public void ApplyLocal_WhenThisComputerIsNotSet_KeepsStoredIsLocalAndNewServerIsNotLocal()
    {
        // Act
        _sut.ApplyLocal("Merinson", new StsServerDataModel { Name = "Merinson" });

        // Assert
        Assert.False(_context.Parameters.Servers["Merinson"].IsLocal);
        Assert.True(_context.Parameters.Servers["PAZISI"].IsLocal);
    }

    [Fact]
    public void ApplyLocal_WhenRecordExists_UpdatesTheSameObject()
    {
        // Arrange
        _context.Parameters.CurrentMachineServerName = "PAZISI";
        ServerDataModel existing = _context.Parameters.Servers["PAZISI"];

        // Act
        _sut.ApplyLocal("PAZISI", new StsServerDataModel { Name = "PAZISI", Runtime = "win-arm64" });

        // Assert
        Assert.Same(existing, _context.Parameters.Servers["PAZISI"]);
        Assert.Equal("win-arm64", existing.Runtime);
        Assert.True(existing.IsLocal);
    }

    [Fact]
    public void GetLocalRecords_WhenCalled_SendsNoIsLocal()
    {
        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        var server = Assert.IsType<StsServerDataModel>(result["PAZISI"]);
        Assert.Equal("win-x64", server.Runtime);
        Assert.Null(typeof(StsServerDataModel).GetProperty(nameof(ServerDataModel.IsLocal)));
    }
}
