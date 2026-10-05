using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//ბაზის სერვერებთან კავშირები: SupportToolsParameters.DatabaseServerConnections ↔ StsDatabaseServerConnectionDataModel
//(B3), folders set-ებით. მომხმარებელი და პაროლი საიდუმლოა და არსად იბეჭდება. კომპიუტერისთვის განსხვავებული კავშირი
//(მაგ. Linux-ზე) სინქრონიზაციიდან ჩანაწერის დონეზე გამოირიცხება (RegistrySyncState-ის ExcludedKeys)
public sealed class DatabaseServerConnectionsRegistrySyncAdapter : DictionaryRegistrySyncAdapter<
    DatabaseServerConnectionData, StsDatabaseServerConnectionDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseServerConnectionsRegistrySyncAdapter(SupportToolsServerApiClient apiClient,
        SupportToolsParameters parameters, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.DatabaseServerConnections;
    public override int Order => RegistryCollections.DatabaseServerConnectionsOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteDatabaseServerConnection(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, DatabaseServerConnectionData> GetLocalDictionary()
    {
        return _parameters.DatabaseServerConnections;
    }

    protected override StsDatabaseServerConnectionDataModel ToContract(string key, DatabaseServerConnectionData local)
    {
        return DatabaseServerConnectionMapper.ToContract(key, local);
    }

    protected override DatabaseServerConnectionData ToLocal(StsDatabaseServerConnectionDataModel contract,
        DatabaseServerConnectionData? existing)
    {
        DatabaseServerConnectionData connection = existing ?? new DatabaseServerConnectionData();
        DatabaseServerConnectionMapper.ApplyToLocal(contract, connection);
        return connection;
    }

    protected override StsDatabaseServerConnectionDataModel NormalizeContract(
        StsDatabaseServerConnectionDataModel contract)
    {
        return DatabaseServerConnectionMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsDatabaseServerConnectionDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetDatabaseServerConnections(cancellationToken);
    }

    protected override string GetKey(StsDatabaseServerConnectionDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsDatabaseServerConnectionDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsDatabaseServerConnectionDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateDatabaseServerConnection(key, contract, cancellationToken);
    }

    protected override string? FindUnsupportedField(StsDatabaseServerConnectionDataModel contract)
    {
        return DatabaseServerConnectionMapper.HasUnknownProvider(contract)
            ? nameof(StsDatabaseServerConnectionDataModel.DatabaseServerProvider)
            : null;
    }
}
