using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//სერვერები: SupportToolsParameters.Servers ↔ StsServerDataModel (B4). IsLocal არ სინქრონიზდება: ApplyLocal-ის შემდეგ ის
//ამ კომპიუტერის CurrentMachineServerName-ით თავიდან ითვლება (G6, ServersIsLocalCalculator)
public sealed class ServersRegistrySyncAdapter : DictionaryRegistrySyncAdapter<ServerDataModel, StsServerDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServersRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.Servers;
    public override int Order => RegistryCollections.ServersOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteServer(key, expectedVersion, cancellationToken);
    }

    protected override void ApplyContract(string key, StsServerDataModel contract)
    {
        base.ApplyContract(key, contract);
        ServersIsLocalCalculator.Recalculate(_parameters);
    }

    protected override Dictionary<string, ServerDataModel> GetLocalDictionary()
    {
        return _parameters.Servers;
    }

    protected override StsServerDataModel ToContract(string key, ServerDataModel local)
    {
        return ServerMapper.ToContract(key, local);
    }

    protected override ServerDataModel ToLocal(StsServerDataModel contract, ServerDataModel? existing)
    {
        ServerDataModel server = existing ?? new ServerDataModel();
        ServerMapper.ApplyToLocal(contract, server);
        return server;
    }

    protected override StsServerDataModel NormalizeContract(StsServerDataModel contract)
    {
        return ServerMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsServerDataModel>>> GetServerContracts(CancellationToken cancellationToken)
    {
        return _apiClient.GetServers(cancellationToken);
    }

    protected override string GetKey(StsServerDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsServerDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsServerDataModel contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateServer(key, contract, cancellationToken);
    }
}
