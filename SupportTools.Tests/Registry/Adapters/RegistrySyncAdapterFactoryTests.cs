using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
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

    //every collection of C3 in the dependency order of the task and Projects (C4) last
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
            RegistryCollections.ProjectCreatorSettings, RegistryCollections.Projects
        ], result.OrderBy(x => x.Order).Select(x => x.CollectionName));
    }

    //the collection name is the key of the sync state
    [Fact]
    public void CreateAdapters_WhenCalled_GivesEveryAdapterAUniqueNameAndOrder()
    {
        // Act
        List<IRegistrySyncAdapter> result = CreateAdapters();

        // Assert
        Assert.Equal(result.Count,
            result.Select(x => x.CollectionName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
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
    [InlineData(RegistryCollections.Gits, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.NpmPackages, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.EditorConfigPatterns, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.DatabaseServerConnections, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.SmartSchemas, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.FileStorages, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.Servers, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.Environments, RegistryCollections.Projects)]
    [InlineData(RegistryCollections.ApiClients, RegistryCollections.Projects)]
    public void CreateAdapters_WhenCollectionReferencesAnother_OrdersTheReferencedOneFirst(string referenced,
        string referencing)
    {
        // Act
        List<IRegistrySyncAdapter> result = CreateAdapters();

        // Assert
        Assert.True(result.Single(x => x.CollectionName == referenced).Order <
                    result.Single(x => x.CollectionName == referencing).Order);
    }

    //the Projects adapter checks its references against the server keys of the factory's own adapters: each of the
    //four referenced collections is read before Projects, and only its keys answer for its references
    [Fact]
    public async Task CreateAdapters_WhenReferencedRecordsAreOnlyOnServer_GivesProjectsTheirServerKeys()
    {
        // Arrange
        _context.Parameters.Projects["AppFake"] = new ProjectModel
        {
            GitProjectNames = ["AppFake"],
            FrontNpmPackageNames = ["react"],
            EditorConfigPatternName = "default",
            DevDatabaseParameters = new DatabaseParameters { DbConnectionName = "Dev" }
        };
        _context.Server.Store(FakeSupportToolsServer.GitRepos, "AppFake",
            new StsGitDataModel
            {
                GitProjectName = "AppFake",
                GitProjectAddress = "git@github.com:fake/AppFake.git",
                GitProjectFolderName = "AppFake",
                GitIgnorePatternName = "CSharp"
            }, 1);
        _context.Server.Store("npmpackages", "react", new StsNpmPackageDataModel { Name = "react" }, 1);
        _context.Server.Store(FakeSupportToolsServer.EditorConfigFileTypes, "default",
            new StsEditorConfigFileTypeDataModel { Name = "default", Content = "root = true" }, 1);
        _context.Server.Store("databaseserverconnections", "Dev",
            new StsDatabaseServerConnectionDataModel { Name = "Dev", DatabaseServerProvider = "SqlServer" }, 1);
        List<IRegistrySyncAdapter> adapters = CreateAdapters();
        foreach (IRegistrySyncAdapter adapter in adapters.OrderBy(x => x.Order))
        {
            await adapter.GetServerRecords(default);
        }

        // Act
        adapters.Single(x => x.CollectionName == RegistryCollections.Projects).GetLocalRecords();

        // Assert
        Assert.Empty(_context.Warnings.Items);
    }

    private List<IRegistrySyncAdapter> CreateAdapters()
    {
        return RegistrySyncAdapterFactory.CreateAdapters(_context.ApiClient, _context.Parameters, _context.PathMapper,
            _context.Warnings);
    }
}
