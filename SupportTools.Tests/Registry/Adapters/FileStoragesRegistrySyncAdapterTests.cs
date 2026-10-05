using System;
using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Adapters;
using ParametersManagement.LibFileParameters.Models;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//a Linux computer: its local storage path reaches the server in the canonical form and comes back in its own
[Collection(ConsoleCaptureCollection.Name)]
public sealed class FileStoragesRegistrySyncAdapterTests : IDisposable
{
    private const string CanonicalStoragePath = @"D:\1WorkDotnet\Backups";
    private const string LinuxStoragePath = "/home/u/1WorkDotnet/Backups";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly FileStoragesRegistrySyncAdapter _sut;

    public FileStoragesRegistrySyncAdapterTests()
    {
        _context.PathMapper = MapperTestHelpers.LinuxPathMapper();
        _sut = new FileStoragesRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.PathMapper,
            _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void GetLocalRecords_WhenComputerIsLinux_GivesCanonicalPath()
    {
        // Arrange
        _context.Parameters.FileStorages["Backups"] =
            new FileStorageData { FileStoragePath = LinuxStoragePath, Password = "fake-password" };

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        var fileStorage = Assert.IsType<StsFileStorageDataModel>(result["Backups"]);
        Assert.Equal(CanonicalStoragePath, fileStorage.FileStoragePath);
        Assert.Equal("fake-password", fileStorage.Password);
    }

    [Fact]
    public void ApplyLocal_WhenComputerIsLinux_StoresLocalPath()
    {
        // Act
        _sut.ApplyLocal("Backups",
            new StsFileStorageDataModel { Name = "Backups", FileStoragePath = CanonicalStoragePath });

        // Assert
        Assert.Equal(LinuxStoragePath, _context.Parameters.FileStorages["Backups"].FileStoragePath);
        Assert.Empty(_context.PathMapper.Issues);
    }
}
