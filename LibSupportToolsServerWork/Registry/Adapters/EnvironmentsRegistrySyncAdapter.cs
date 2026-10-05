using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//გარემოები: SupportToolsParameters.Environments (სახელი → აღწერა) ↔ StsEnvironmentDataModel (B1)
public sealed class EnvironmentsRegistrySyncAdapter : DictionaryRegistrySyncAdapter<string, StsEnvironmentDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EnvironmentsRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.Environments;
    public override int Order => RegistryCollections.EnvironmentsOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteEnvironment(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, string> GetLocalDictionary()
    {
        return _parameters.Environments;
    }

    protected override StsEnvironmentDataModel ToContract(string key, string local)
    {
        return EnvironmentMapper.ToContract(key, local);
    }

    protected override string ToLocal(StsEnvironmentDataModel contract, string? existing)
    {
        return EnvironmentMapper.ToLocal(contract);
    }

    protected override StsEnvironmentDataModel NormalizeContract(StsEnvironmentDataModel contract)
    {
        return EnvironmentMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsEnvironmentDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetEnvironments(cancellationToken);
    }

    protected override string GetKey(StsEnvironmentDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsEnvironmentDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsEnvironmentDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateEnvironment(key, contract, cancellationToken);
    }
}
