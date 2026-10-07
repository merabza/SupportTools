using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using SupportTools.Menu.SyncRegistry;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using Xunit;

namespace SupportTools.Tests.Menu.SyncRegistry;

public sealed class RegistrySyncPreflightTests
{
    //every collection whose records have keys
    public static TheoryData<string> CollectionsWithKeys =>
    [
        RegistryCollections.Environments, RegistryCollections.RunTimes, RegistryCollections.NpmPackages,
        RegistryCollections.ReactAppTemplates, RegistryCollections.DotnetTools, RegistryCollections.SmartSchemas,
        RegistryCollections.FileStorages, RegistryCollections.ApiClients,
        RegistryCollections.DatabaseServerConnections, RegistryCollections.Servers,
        RegistryCollections.GitIgnorePatterns, RegistryCollections.Gits, RegistryCollections.EditorConfigPatterns,
        RegistryCollections.ProjectTemplates, RegistryCollections.Projects
    ];

    [Theory]
    [MemberData(nameof(CollectionsWithKeys))]
    public void FindKeysDifferingOnlyByCase_WhenTwoKeysDifferOnlyByCase_ReportsThem(string collectionName)
    {
        // Arrange
        var parameters = new SupportToolsParameters();
        AddKey(parameters, collectionName, "AppFake");
        AddKey(parameters, collectionName, "appfake");

        // Act
        List<string> result = RegistrySyncPreflight.FindKeysDifferingOnlyByCase(parameters);

        // Assert
        Assert.Equal([$"{collectionName}: AppFake / appfake"], result);
    }

    [Fact]
    public void FindKeysDifferingOnlyByCase_WhenKeysAreUnique_ReportsNothing()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            Gits = { ["AppA"] = new GitDataModel(), ["AppB"] = new GitDataModel() },
            Projects = { ["AppA"] = new ProjectModel() },
            GitIgnorePatterns = { "CSharp", "React" }
        };

        // Act
        List<string> result = RegistrySyncPreflight.FindKeysDifferingOnlyByCase(parameters);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindKeysDifferingOnlyByCase_WhenSeveralCollectionsHaveSuchKeys_ReportsEveryGroupInCollectionOrder()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            Projects = { ["AppX"] = new ProjectModel(), ["APPX"] = new ProjectModel() },
            Gits = { ["AppA"] = new GitDataModel(), ["appA"] = new GitDataModel(), ["APPA"] = new GitDataModel() },
            Servers = { ["PAZISI"] = new ServerDataModel(), ["Pazisi"] = new ServerDataModel() }
        };

        // Act
        List<string> result = RegistrySyncPreflight.FindKeysDifferingOnlyByCase(parameters);

        // Assert
        Assert.Equal(["Servers: PAZISI / Pazisi", "Gits: AppA / appA / APPA", "Projects: AppX / APPX"], result);
    }

    //a new collection adapter needs its local keys in the preflight too. The singletons have no keys, and the keys of
    //the stored files are paths: paths that differ only by case are one file on Windows, and the adapter merges them
    [Fact]
    public void GetLocalKeys_WhenCalled_CoversEveryCollectionOfTheAdapterFactoryExceptTheSingletonsAndStoredFiles()
    {
        // Arrange
        var apiClient = new SupportToolsServerApiClient(null, new Mock<IHttpClientFactory>().Object,
            "http://127.0.0.1:0/api/v1", null, false);
        var parameters = new SupportToolsParameters();
        List<string> expected =
        [
            .. RegistrySyncAdapterFactory
                .CreateAdapters(apiClient, parameters, new PathMapper([]), new RegistrySyncWarnings())
                .OrderBy(x => x.Order).Select(x => x.CollectionName).Except([
                    RegistryCollections.GlobalSettings, RegistryCollections.ProjectCreatorSettings,
                    RegistryCollections.StoredFiles
                ])
        ];

        // Act
        List<(string CollectionName, IEnumerable<string> Keys)> result = RegistrySyncPreflight.GetLocalKeys(parameters);

        // Assert
        Assert.Equal(expected, result.Select(x => x.CollectionName));
    }

    private static void AddKey(SupportToolsParameters parameters, string collectionName, string key)
    {
        switch (collectionName)
        {
            case RegistryCollections.Environments:
                parameters.Environments[key] = "Fake";
                break;
            case RegistryCollections.RunTimes:
                parameters.RunTimes[key] = "Fake";
                break;
            case RegistryCollections.NpmPackages:
                parameters.NpmPackages[key] = "Fake";
                break;
            case RegistryCollections.ReactAppTemplates:
                parameters.ReactAppTemplates[key] = "Fake";
                break;
            case RegistryCollections.DotnetTools:
                parameters.DotnetTools[key] = new DotnetToolData();
                break;
            case RegistryCollections.SmartSchemas:
                parameters.SmartSchemas[key] = new SmartSchema();
                break;
            case RegistryCollections.FileStorages:
                parameters.FileStorages[key] = new FileStorageData();
                break;
            case RegistryCollections.ApiClients:
                parameters.ApiClients[key] = new ApiClientSettings();
                break;
            case RegistryCollections.DatabaseServerConnections:
                parameters.DatabaseServerConnections[key] = new DatabaseServerConnectionData();
                break;
            case RegistryCollections.Servers:
                parameters.Servers[key] = new ServerDataModel();
                break;
            case RegistryCollections.GitIgnorePatterns:
                parameters.GitIgnorePatterns.Add(key);
                break;
            case RegistryCollections.Gits:
                parameters.Gits[key] = new GitDataModel();
                break;
            case RegistryCollections.EditorConfigPatterns:
                parameters.EditorConfigPatterns.Add(key);
                break;
            case RegistryCollections.ProjectTemplates:
                parameters.AppProjectCreatorAllParameters ??= new AppProjectCreatorAllParameters();
                parameters.AppProjectCreatorAllParameters.Templates[key] = new TemplateModel();
                break;
            case RegistryCollections.Projects:
                parameters.Projects[key] = new ProjectModel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(collectionName), collectionName, null);
        }
    }
}
