using System;
using LibSupportToolsServerWork.Registry.Adapters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class DotnetToolsRegistrySyncAdapterTests : IDisposable
{
    private readonly RegistryAdapterTestContext _context = new();
    private readonly DotnetToolsRegistrySyncAdapter _sut;

    public DotnetToolsRegistrySyncAdapterTests()
    {
        _sut = new DotnetToolsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    //the installed and latest versions and the command belong to this computer
    [Fact]
    public void ApplyLocal_WhenRecordExists_KeepsMachineFields()
    {
        // Arrange
        var existing = new DotnetToolData
        {
            PackageId = "dotnet-ef", InstalledVersion = "8.0.1", LatestVersion = "10.0.0", CommandName = "dotnet ef"
        };
        _context.Parameters.DotnetTools["dotnet-ef"] = existing;

        // Act
        _sut.ApplyLocal("dotnet-ef",
            new StsDotnetToolDataModel { Name = "dotnet-ef", PackageId = "dotnet-ef", MaxVersion = "9.0.0" });

        // Assert
        Assert.Same(existing, _context.Parameters.DotnetTools["dotnet-ef"]);
        Assert.Equal("9.0.0", existing.MaxVersion);
        Assert.Equal("8.0.1", existing.InstalledVersion);
        Assert.Equal("10.0.0", existing.LatestVersion);
        Assert.Equal("dotnet ef", existing.CommandName);
    }

    [Fact]
    public void ApplyLocal_WhenRecordIsNew_LeavesMachineFieldsEmpty()
    {
        // Act
        _sut.ApplyLocal("dotnet-ef", new StsDotnetToolDataModel { Name = "dotnet-ef", PackageId = "dotnet-ef" });

        // Assert
        DotnetToolData result = _context.Parameters.DotnetTools["dotnet-ef"];
        Assert.Equal("dotnet-ef", result.PackageId);
        Assert.Null(result.InstalledVersion);
        Assert.Null(result.CommandName);
    }
}
