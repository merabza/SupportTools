using System;
using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class RegistrySyncAdapterFactoryTests : IDisposable
{
    private readonly RegistryAdapterTestContext _context = new();

    public void Dispose()
    {
        _context.Dispose();
    }

    //every collection of C3 in the dependency order of the task; Projects (C4) comes last
    [Fact]
    public void CreateAdapters_WhenCalled_CreatesAnAdapterForEveryCollectionInDependencyOrder()
    {
        // Act
        List<IRegistrySyncAdapter> result = CreateAdapters();

        // Assert
        Assert.Equal([
            RegistryCollections.Environments, RegistryCollections.RunTimes, RegistryCollections.NpmPackages,
            RegistryCollections.ReactAppTemplates, RegistryCollections.DotnetTools, RegistryCollections.SmartSchemas,
            RegistryCollections.FileStorages, RegistryCollections.ApiClients,
            RegistryCollections.DatabaseServerConnections, RegistryCollections.Servers,
            RegistryCollections.GitIgnorePatterns, RegistryCollections.Gits, RegistryCollections.EditorConfigPatterns,
            RegistryCollections.ProjectTemplates, RegistryCollections.GlobalSettings,
            RegistryCollections.ProjectCreatorSettings
        ], result.OrderBy(x => x.Order).Select(x => x.CollectionName));
    }

    //the collection name is the key of the sync state
    [Fact]
    public void CreateAdapters_WhenCalled_GivesEveryAdapterAUniqueNameAndOrder()
    {
        // Act
        List<IRegistrySyncAdapter> result = CreateAdapters();

        // Assert
        Assert.Equal(result.Count, result.Select(x => x.CollectionName).Distinct(StringComparer.OrdinalIgnoreCase)
            .Count());
        Assert.Equal(result.Count, result.Select(x => x.Order).Distinct().Count());
    }

    //a referenced collection is pushed before and deleted after the collections that reference it
    [Theory]
    [InlineData(RegistryCollections.ApiClients, RegistryCollections.DatabaseServerConnections)]
    [InlineData(RegistryCollections.ApiClients, RegistryCollections.Servers)]
    [InlineData(RegistryCollections.RunTimes, RegistryCollections.Servers)]
    [InlineData(RegistryCollections.GitIgnorePatterns, RegistryCollections.Gits)]
    [InlineData(RegistryCollections.ReactAppTemplates, RegistryCollections.ProjectTemplates)]
    [InlineData(RegistryCollections.FileStorages, RegistryCollections.GlobalSettings)]
    [InlineData(RegistryCollections.SmartSchemas, RegistryCollections.GlobalSettings)]
    [InlineData(RegistryCollections.ApiClients, RegistryCollections.GlobalSettings)]
    [InlineData(RegistryCollections.Servers, RegistryCollections.ProjectCreatorSettings)]
    [InlineData(RegistryCollections.Environments, RegistryCollections.ProjectCreatorSettings)]
    [InlineData(RegistryCollections.DatabaseServerConnections, RegistryCollections.ProjectCreatorSettings)]
    [InlineData(RegistryCollections.FileStorages, RegistryCollections.ProjectCreatorSettings)]
    [InlineData(RegistryCollections.SmartSchemas, RegistryCollections.ProjectCreatorSettings)]
    public void CreateAdapters_WhenCollectionReferencesAnother_OrdersTheReferencedOneFirst(string referenced,
        string referencing)
    {
        // Act
        List<IRegistrySyncAdapter> result = CreateAdapters();

        // Assert
        Assert.True(result.Single(x => x.CollectionName == referenced).Order <
                    result.Single(x => x.CollectionName == referencing).Order);
    }

    private List<IRegistrySyncAdapter> CreateAdapters()
    {
        return RegistrySyncAdapterFactory.CreateAdapters(_context.ApiClient, _context.Parameters, _context.PathMapper,
            _context.Warnings);
    }
}
