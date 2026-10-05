using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibApiClientParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//API კლიენტები: SupportToolsParameters.ApiClients ↔ StsApiClientDataModel (B3). ApiKey საიდუმლოა და არსად იბეჭდება.
//ApiClient, რომელსაც SupportToolsServerWebApiClientName ასახელებს, ამ კომპიუტერისაა (G6, bootstrap): მისით
//სინქრონიზაცია სერვერს უკავშირდება, ამიტომ ის არც იტვირთება და არც ჩამოდის; სერვერის იმავე სახელის ჩანაწერიც
//გამოირიცხება
public sealed class ApiClientsRegistrySyncAdapter : DictionaryRegistrySyncAdapter<ApiClientSettings,
    StsApiClientDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiClientsRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.ApiClients;
    public override int Order => RegistryCollections.ApiClientsOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteApiClient(key, expectedVersion, cancellationToken);
    }

    protected override bool IsSynced(string key)
    {
        return !string.Equals(key, _parameters.SupportToolsServerWebApiClientName,
            StringComparison.OrdinalIgnoreCase);
    }

    protected override Dictionary<string, ApiClientSettings> GetLocalDictionary()
    {
        return _parameters.ApiClients;
    }

    protected override StsApiClientDataModel ToContract(string key, ApiClientSettings local)
    {
        return ApiClientMapper.ToContract(key, local);
    }

    protected override ApiClientSettings ToLocal(StsApiClientDataModel contract, ApiClientSettings? existing)
    {
        ApiClientSettings apiClient = existing ?? new ApiClientSettings();
        ApiClientMapper.ApplyToLocal(contract, apiClient);
        return apiClient;
    }

    protected override StsApiClientDataModel NormalizeContract(StsApiClientDataModel contract)
    {
        return ApiClientMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsApiClientDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetApiClients(cancellationToken);
    }

    protected override string GetKey(StsApiClientDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsApiClientDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsApiClientDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateApiClient(key, contract, cancellationToken);
    }
}
