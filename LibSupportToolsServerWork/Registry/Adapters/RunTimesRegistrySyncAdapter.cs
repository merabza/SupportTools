using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//Runtime Identifier-ები: SupportToolsParameters.RunTimes (სახელი → აღწერა) ↔ StsRuntimeDataModel (B2)
public sealed class RunTimesRegistrySyncAdapter : DictionaryRegistrySyncAdapter<string, StsRuntimeDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RunTimesRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.RunTimes;
    public override int Order => RegistryCollections.RunTimesOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteRuntime(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, string> GetLocalDictionary()
    {
        return _parameters.RunTimes;
    }

    protected override StsRuntimeDataModel ToContract(string key, string local)
    {
        return RuntimeMapper.ToContract(key, local);
    }

    protected override string ToLocal(StsRuntimeDataModel contract, string? existing)
    {
        return RuntimeMapper.ToLocal(contract);
    }

    protected override StsRuntimeDataModel NormalizeContract(StsRuntimeDataModel contract)
    {
        return RuntimeMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsRuntimeDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetRuntimes(cancellationToken);
    }

    protected override string GetKey(StsRuntimeDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsRuntimeDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsRuntimeDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateRuntime(key, contract, cancellationToken);
    }
}
