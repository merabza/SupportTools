using System;
using System.IO;
using System.Threading.Tasks;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Models;

//the sync state is saved and loaded with the parameters file by ParametersManager and ParametersLoader
public sealed class RegistrySyncStateModelTests : IDisposable
{
    private readonly string _parametersFileName;
    private readonly string _rootFolder;

    public RegistrySyncStateModelTests()
    {
        _rootFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        _parametersFileName = Path.Combine(_rootFolder, "SupportTools.json");
    }

    public void Dispose()
    {
        Directory.Delete(_rootFolder, true);
    }

    [Fact]
    public async Task SaveThenLoad_WhenRegistrySyncStateIsSet_KeepsIt()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            RegistrySyncState = { LastSyncUtc = new DateTime(2026, 10, 2, 12, 30, 15, DateTimeKind.Utc) }
        };
        RegistryCollectionSyncStateModel environments = parameters.RegistrySyncState.GetOrAddCollection("Environments");
        environments.Records["Dev"] = new RegistryRecordSyncStateModel { Version = 3, Hash = "ABC" };
        environments.ExcludedKeys.Add("LinuxDb");
        var parametersManager = new ParametersManager(null, parameters);

        // Act
        await parametersManager.Save(parameters, null, _parametersFileName);
        SupportToolsParameters loaded = Load();

        // Assert
        RegistrySyncStateModel state = loaded.RegistrySyncState;
        Assert.Equal(new DateTime(2026, 10, 2, 12, 30, 15, DateTimeKind.Utc), state.LastSyncUtc);
        Assert.Equal(DateTimeKind.Utc, state.LastSyncUtc?.Kind);
        RegistryCollectionSyncStateModel loadedEnvironments = Assert.Single(state.Collections).Value;
        RegistryRecordSyncStateModel record = Assert.Single(loadedEnvironments.Records).Value;
        Assert.Equal(3, record.Version);
        Assert.Equal("ABC", record.Hash);
        Assert.Equal("LinuxDb", Assert.Single(loadedEnvironments.ExcludedKeys));
    }

    //G8: Newtonsoft fills the dictionaries that the initializers created, so the loaded keys keep comparing
    //without case
    [Fact]
    public async Task Load_WhenFileHasRegistrySyncState_ComparesKeysCaseInsensitively()
    {
        // Arrange
        await File.WriteAllTextAsync(_parametersFileName, """
                                                          {
                                                            "RegistrySyncState": {
                                                              "LastSyncUtc": "2026-10-02T12:30:15Z",
                                                              "Collections": {
                                                                "Environments": {
                                                                  "Records": { "Dev": { "Version": 3, "Hash": "ABC" } },
                                                                  "ExcludedKeys": [ "LinuxDb" ]
                                                                }
                                                              }
                                                            }
                                                          }
                                                          """);

        // Act
        SupportToolsParameters loaded = Load();

        // Assert
        RegistrySyncStateModel state = loaded.RegistrySyncState;
        Assert.True(state.Collections.TryGetValue("ENVIRONMENTS", out RegistryCollectionSyncStateModel? environments));
        Assert.True(environments.Records.TryGetValue("dev", out RegistryRecordSyncStateModel? record));
        Assert.Equal(3, record.Version);
        Assert.Contains("LINUXDB", environments.ExcludedKeys);
        Assert.Same(environments, state.GetOrAddCollection("environments"));
    }

    //a parameters file written before the sync state existed must still load
    [Fact]
    public async Task Load_WhenFileHasNoRegistrySyncState_UsesEmptyState()
    {
        // Arrange
        await File.WriteAllTextAsync(_parametersFileName, """{ "LogFolder": "D:\\Logs" }""");

        // Act
        SupportToolsParameters loaded = Load();

        // Assert
        Assert.Null(loaded.RegistrySyncState.LastSyncUtc);
        Assert.Empty(loaded.RegistrySyncState.Collections);
    }

    [Fact]
    public void GetOrAddCollection_WhenCollectionIsMissing_AddsEmptyCollection()
    {
        // Arrange
        var sut = new RegistrySyncStateModel();

        // Act
        RegistryCollectionSyncStateModel result = sut.GetOrAddCollection("Servers");

        // Assert
        Assert.Same(result, sut.Collections["Servers"]);
        Assert.Empty(result.Records);
        Assert.Empty(result.ExcludedKeys);
    }

    [Fact]
    public void GetOrAddCollection_WhenCollectionExistsWithOtherCase_ReturnsIt()
    {
        // Arrange
        var sut = new RegistrySyncStateModel();
        RegistryCollectionSyncStateModel servers = sut.GetOrAddCollection("Servers");

        // Act
        RegistryCollectionSyncStateModel result = sut.GetOrAddCollection("SERVERS");

        // Assert
        Assert.Same(servers, result);
        Assert.Single(sut.Collections);
    }

    private SupportToolsParameters Load()
    {
        var parametersLoader = new ParametersLoader<SupportToolsParameters>();
        Assert.True(parametersLoader.TryLoadParameters(_parametersFileName));
        return Assert.IsType<SupportToolsParameters>(parametersLoader.Par);
    }
}
