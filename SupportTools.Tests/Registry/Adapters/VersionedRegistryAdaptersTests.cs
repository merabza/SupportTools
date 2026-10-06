using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseTools.DbTools.Models;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the adapters of the registry areas with B1's routes (GET list, POST update/{key} with the expected version,
//DELETE delete/{key}?version=N) share their plumbing: every case runs the same behaviors against the fake server
[Collection(ConsoleCaptureCollection.Name)]
public sealed class VersionedRegistryAdaptersTests : IDisposable
{
    //a scoped npm package name holds a slash, which the client escapes and the server decodes
    private const string NpmPackageName = "@testing-library/react";

    private static readonly Dictionary<string, AdapterCase> Cases = new(StringComparer.Ordinal)
    {
        [RegistryCollections.Environments] =
            new AdapterCase
            {
                Area = "environments",
                Order = RegistryCollections.EnvironmentsOrder,
                CreateAdapter = x => new EnvironmentsRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract = name => new StsEnvironmentDataModel { Name = name, Description = "server" },
                AddLocal = (parameters, name) => parameters.Environments[name] = "local",
                LocalKeys = parameters => parameters.Environments.Keys
            },
        [RegistryCollections.RunTimes] =
            new AdapterCase
            {
                Area = "runtimes",
                Order = RegistryCollections.RunTimesOrder,
                CreateAdapter = x => new RunTimesRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract = name => new StsRuntimeDataModel { Name = name, Description = "server" },
                AddLocal = (parameters, name) => parameters.RunTimes[name] = "local",
                LocalKeys = parameters => parameters.RunTimes.Keys
            },
        [RegistryCollections.NpmPackages] =
            new AdapterCase
            {
                Area = "npmpackages",
                Order = RegistryCollections.NpmPackagesOrder,
                RecordName = NpmPackageName,
                CreateAdapter = x => new NpmPackagesRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract = name => new StsNpmPackageDataModel { Name = name, Description = "server" },
                AddLocal = (parameters, name) => parameters.NpmPackages[name] = "local",
                LocalKeys = parameters => parameters.NpmPackages.Keys
            },
        [RegistryCollections.ReactAppTemplates] =
            new AdapterCase
            {
                Area = "reactapptemplates",
                Order = RegistryCollections.ReactAppTemplatesOrder,
                CreateAdapter =
                    x => new ReactAppTemplatesRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract =
                    name => new StsReactAppTemplateDataModel { Name = name, Template = "cra-template-server" },
                AddLocal = (parameters, name) => parameters.ReactAppTemplates[name] = "cra-template-local",
                LocalKeys = parameters => parameters.ReactAppTemplates.Keys
            },
        [RegistryCollections.DotnetTools] =
            new AdapterCase
            {
                Area = "dotnettools",
                Order = RegistryCollections.DotnetToolsOrder,
                CreateAdapter = x => new DotnetToolsRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract = name => new StsDotnetToolDataModel { Name = name, PackageId = "server-package" },
                AddLocal =
                    (parameters, name) =>
                        parameters.DotnetTools[name] = new DotnetToolData { PackageId = "local-package" },
                LocalKeys = parameters => parameters.DotnetTools.Keys,
                LocalRecord = (parameters, name) => parameters.DotnetTools[name]
            },
        [RegistryCollections.SmartSchemas] =
            new AdapterCase
            {
                Area = "smartschemas",
                Order = RegistryCollections.SmartSchemasOrder,
                CreateAdapter = x => new SmartSchemasRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract =
                    name => new StsSmartSchemaDataModel
                    {
                        Name = name,
                        LastPreserveCount = 2,
                        Details = [new StsSmartSchemaDetailDataModel { PeriodType = "Day", PreserveCount = 7 }]
                    },
                AddLocal =
                    (parameters, name) => parameters.SmartSchemas[name] = new SmartSchema
                    {
                        LastPreserveCount = 1,
                        Details = [new SmartSchemaDetail { PeriodType = EPeriodType.Month, PreserveCount = 3 }]
                    },
                LocalKeys = parameters => parameters.SmartSchemas.Keys
            },
        [RegistryCollections.FileStorages] =
            new AdapterCase
            {
                Area = "filestorages",
                Order = RegistryCollections.FileStoragesOrder,
                CreateAdapter =
                    x => new FileStoragesRegistrySyncAdapter(x.ApiClient, x.Parameters, x.PathMapper, x.Warnings),
                NewContract =
                    name => new StsFileStorageDataModel
                    {
                        Name = name,
                        FileStoragePath = "ftp://files.example.test/server",
                        Password = "fake-password"
                    },
                AddLocal =
                    (parameters, name) => parameters.FileStorages[name] =
                        new FileStorageData { FileStoragePath = @"D:\Local\Storage" },
                LocalKeys = parameters => parameters.FileStorages.Keys,
                LocalRecord = (parameters, name) => parameters.FileStorages[name]
            },
        [RegistryCollections.ApiClients] =
            new AdapterCase
            {
                Area = "apiclients",
                Order = RegistryCollections.ApiClientsOrder,
                CreateAdapter = x => new ApiClientsRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract =
                    name => new StsApiClientDataModel
                    {
                        Name = name, Server = "http://server.example.test/api", ApiKey = "fake-server-key"
                    },
                AddLocal =
                    (parameters, name) => parameters.ApiClients[name] = new ApiClientSettings
                    {
                        Server = "http://local.example.test/api", ApiKey = "fake-local-key"
                    },
                LocalKeys = parameters => parameters.ApiClients.Keys,
                LocalRecord = (parameters, name) => parameters.ApiClients[name]
            },
        [RegistryCollections.DatabaseServerConnections] =
            new AdapterCase
            {
                Area = "databaseserverconnections",
                Order = RegistryCollections.DatabaseServerConnectionsOrder,
                CreateAdapter =
                    x => new DatabaseServerConnectionsRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract =
                    name => new StsDatabaseServerConnectionDataModel
                    {
                        Name = name,
                        DatabaseServerProvider = "SqlServer",
                        ServerAddress = @"(localdb)\Server",
                        DatabaseFoldersSets =
                            [new StsDatabaseFoldersSetDataModel { Name = "Default", Backup = @"D:\B" }]
                    },
                AddLocal =
                    (parameters, name) => parameters.DatabaseServerConnections[name] =
                        new DatabaseServerConnectionData
                        {
                            DatabaseServerProvider = EDatabaseProvider.SqLite,
                            DatabaseFoldersSets =
                                new Dictionary<string, DatabaseFoldersSet> { ["Local"] = new() }
                        },
                LocalKeys = parameters => parameters.DatabaseServerConnections.Keys,
                LocalRecord = (parameters, name) => parameters.DatabaseServerConnections[name]
            },
        [RegistryCollections.Servers] =
            new AdapterCase
            {
                Area = "servers",
                Order = RegistryCollections.ServersOrder,
                CreateAdapter = x => new ServersRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
                NewContract = name => new StsServerDataModel { Name = name, Runtime = "linux-x64" },
                AddLocal =
                    (parameters, name) => parameters.Servers[name] = new ServerDataModel { Runtime = "win-x64" },
                LocalKeys = parameters => parameters.Servers.Keys,
                LocalRecord = (parameters, name) => parameters.Servers[name]
            },
        [RegistryCollections.ProjectTemplates] = new AdapterCase
        {
            Area = "projecttemplates",
            Order = RegistryCollections.ProjectTemplatesOrder,
            CreateAdapter = x => new ProjectTemplatesRegistrySyncAdapter(x.ApiClient, x.Parameters, x.Warnings),
            NewContract = name => new StsProjectTemplateDataModel
            {
                Name = name, SupportProjectType = "Api", UseReact = true
            },
            AddLocal = (parameters, name) =>
            {
                parameters.AppProjectCreatorAllParameters ??= new AppProjectCreatorAllParameters();
                parameters.AppProjectCreatorAllParameters.Templates[name] =
                    new TemplateModel { SupportProjectType = ESupportProjectType.Console };
            },
            LocalKeys = parameters => parameters.AppProjectCreatorAllParameters?.Templates.Keys.ToList() ?? [],
            LocalRecord = (parameters, name) => parameters.AppProjectCreatorAllParameters!.Templates[name]
        }
    };

    private readonly RegistryAdapterTestContext _context = new();

    public static TheoryData<string> AllCases => [.. Cases.Keys];

    //the cases whose local records are objects that ApplyLocal updates in place
    public static TheoryData<string> CasesWithLocalObjects =>
        [.. Cases.Where(x => x.Value.LocalRecord is not null).Select(x => x.Key)];

    public void Dispose()
    {
        _context.Dispose();
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];

        // Act
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Assert
        Assert.Equal((collectionName, adapterCase.Order), (sut.CollectionName, sut.Order));
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task GetServerRecords_WhenServerHasRecord_ReturnsItByNameWithItsVersion(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        _context.Server.Store(adapterCase.Area, adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            3);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await sut.GetServerRecords(default);

        // Assert
        RegistryServerRecord record = Assert.Single(result.Value, x => x.Key == adapterCase.RecordName).Value;
        Assert.Equal(3, record.Version);
        Assert.Equal(adapterCase.NewContract(adapterCase.RecordName).GetType(), record.Contract.GetType());
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task GetServerRecords_WhenServerIsUnavailable_ReturnsRequestFailed(string collectionName)
    {
        // Arrange
        IRegistrySyncAdapter sut = Cases[collectionName].CreateAdapter(_context);
        _context.Server.IsUnavailable = true;

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await sut.GetServerRecords(default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void GetLocalRecords_WhenLocalRecordExists_ReturnsItsContractByLocalKey(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        adapterCase.AddLocal(_context.Parameters, adapterCase.RecordName);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        IReadOnlyDictionary<string, object> result = sut.GetLocalRecords();

        // Assert
        Assert.Equal(adapterCase.NewContract(adapterCase.RecordName).GetType(),
            Assert.Single(result, x => x.Key == adapterCase.RecordName).Value.GetType());
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Upsert_WhenRecordIsNew_CreatesItWithTheFirstVersion(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        Result<int> result = await sut.Upsert(adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            0, default);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.Equal(1, _context.Server.VersionOf(adapterCase.Area, adapterCase.RecordName));
    }

    //the contract's Version is the expected version of B1's upsert
    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Upsert_WhenVersionIsExpected_UpdatesAndReturnsTheNextVersion(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        _context.Server.Store(adapterCase.Area, adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            3);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        Result<int> result = await sut.Upsert(adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            3, default);

        // Assert
        Assert.Equal(4, result.Value);
        Assert.Equal(4, _context.Server.VersionOf(adapterCase.Area, adapterCase.RecordName));
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Upsert_WhenServerHasAnotherVersion_ReturnsConcurrencyConflict(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        _context.Server.Store(adapterCase.Area, adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            4);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        Result<int> result = await sut.Upsert(adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            3, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Delete_WhenVersionIsExpected_DeletesWithTheVersion(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        _context.Server.Store(adapterCase.Area, adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            2);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        Result result = await sut.Delete(adapterCase.RecordName, 2, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Server.Records(adapterCase.Area));
        Assert.Contains(_context.Server.Requests,
            x => x.StartsWith("DELETE", StringComparison.Ordinal) &&
                 x.EndsWith("?version=2", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Delete_WhenServerHasAnotherVersion_ReturnsConcurrencyConflict(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        _context.Server.Store(adapterCase.Area, adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName),
            3);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        Result result = await sut.Delete(adapterCase.RecordName, 2, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void ApplyLocal_WhenRecordIsNew_AddsItToTheLocalRecords(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        sut.ApplyLocal(adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName));

        // Assert
        Assert.Equal([adapterCase.RecordName], adapterCase.LocalKeys(_context.Parameters));
        Assert.Equal(Hash(sut, adapterCase.NewContract(adapterCase.RecordName)),
            Hash(sut, sut.GetLocalRecords()[adapterCase.RecordName]));
    }

    //the round trip gives the server's record: the local record now hashes like the server's
    [Theory]
    [MemberData(nameof(AllCases))]
    public void ApplyLocal_WhenRecordExists_ReplacesItsSharedContent(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        adapterCase.AddLocal(_context.Parameters, adapterCase.RecordName);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        sut.ApplyLocal(adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName));

        // Assert
        Assert.Equal([adapterCase.RecordName], adapterCase.LocalKeys(_context.Parameters));
        Assert.Equal(Hash(sut, adapterCase.NewContract(adapterCase.RecordName)),
            Hash(sut, sut.GetLocalRecords()[adapterCase.RecordName]));
    }

    //open editors keep references to the record objects
    [Theory]
    [MemberData(nameof(CasesWithLocalObjects))]
    public void ApplyLocal_WhenRecordExists_UpdatesTheSameObject(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        adapterCase.AddLocal(_context.Parameters, adapterCase.RecordName);
        object existing = adapterCase.LocalRecord!(_context.Parameters, adapterCase.RecordName);
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        sut.ApplyLocal(adapterCase.RecordName, adapterCase.NewContract(adapterCase.RecordName));

        // Assert
        Assert.Same(existing, adapterCase.LocalRecord(_context.Parameters, adapterCase.RecordName));
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void RemoveLocal_WhenRecordExists_RemovesIt(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        adapterCase.AddLocal(_context.Parameters, adapterCase.RecordName);
        adapterCase.AddLocal(_context.Parameters, "Other");
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);

        // Act
        sut.RemoveLocal(adapterCase.RecordName);

        // Assert
        Assert.Equal(["Other"], adapterCase.LocalKeys(_context.Parameters));
    }

    //the engine checks that normalizing a normalized contract keeps its hash
    [Theory]
    [MemberData(nameof(AllCases))]
    public void Normalize_WhenCalledTwice_KeepsTheHash(string collectionName)
    {
        // Arrange
        AdapterCase adapterCase = Cases[collectionName];
        IRegistrySyncAdapter sut = adapterCase.CreateAdapter(_context);
        object normalized = sut.Normalize(adapterCase.NewContract(adapterCase.RecordName));

        // Act
        object result = sut.Normalize(normalized);

        // Assert
        Assert.Equal(RegistryContractHasher.ComputeHash(normalized), RegistryContractHasher.ComputeHash(result));
    }

    private static string Hash(IRegistrySyncAdapter adapter, object contract)
    {
        return RegistryContractHasher.ComputeHash(adapter.Normalize(contract));
    }

    //one registry area: its route, its adapter and sample records. The local and server samples differ, so a
    //successful ApplyLocal is visible in the hash
    private sealed class AdapterCase
    {
        public required string Area { get; init; }
        public required int Order { get; init; }
        public string RecordName { get; init; } = "Record";
        public required Func<RegistryAdapterTestContext, IRegistrySyncAdapter> CreateAdapter { get; init; }

        //a server contract of the record with this name
        public required Func<string, object> NewContract { get; init; }

        public required Action<SupportToolsParameters, string> AddLocal { get; init; }
        public required Func<SupportToolsParameters, IEnumerable<string>> LocalKeys { get; init; }

        //the local record object; null when the local value is a string or ApplyLocal replaces the object
        public Func<SupportToolsParameters, string, object>? LocalRecord { get; init; }
    }
}
