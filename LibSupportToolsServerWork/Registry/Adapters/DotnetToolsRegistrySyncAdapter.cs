using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//dotnet-ის ხელსაწყოები: SupportToolsParameters.DotnetTools ↔ StsDotnetToolDataModel (B2), მხოლოდ საერთო ველებით.
//დაყენებული და ბოლო ვერსია და ბრძანება ამ კომპიუტერისაა და ApplyLocal-ის შემდეგაც რჩება
public sealed class DotnetToolsRegistrySyncAdapter : DictionaryRegistrySyncAdapter<DotnetToolData,
    StsDotnetToolDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetToolsRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.DotnetTools;
    public override int Order => RegistryCollections.DotnetToolsOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteDotnetTool(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, DotnetToolData> GetLocalDictionary()
    {
        return _parameters.DotnetTools;
    }

    protected override StsDotnetToolDataModel ToContract(string key, DotnetToolData local)
    {
        return DotnetToolMapper.ToContract(key, local);
    }

    protected override DotnetToolData ToLocal(StsDotnetToolDataModel contract, DotnetToolData? existing)
    {
        DotnetToolData dotnetTool = existing ?? new DotnetToolData();
        DotnetToolMapper.ApplyToLocal(contract, dotnetTool);
        return dotnetTool;
    }

    protected override StsDotnetToolDataModel NormalizeContract(StsDotnetToolDataModel contract)
    {
        return DotnetToolMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsDotnetToolDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetDotnetTools(cancellationToken);
    }

    protected override string GetKey(StsDotnetToolDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsDotnetToolDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsDotnetToolDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateDotnetTool(key, contract, cancellationToken);
    }
}
