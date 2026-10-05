using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//npm-ის პაკეტები: SupportToolsParameters.NpmPackages (სახელი → აღწერა) ↔ StsNpmPackageDataModel (B2)
public sealed class NpmPackagesRegistrySyncAdapter : DictionaryRegistrySyncAdapter<string, StsNpmPackageDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NpmPackagesRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.NpmPackages;
    public override int Order => RegistryCollections.NpmPackagesOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteNpmPackage(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, string> GetLocalDictionary()
    {
        return _parameters.NpmPackages;
    }

    protected override StsNpmPackageDataModel ToContract(string key, string local)
    {
        return NpmPackageMapper.ToContract(key, local);
    }

    protected override string ToLocal(StsNpmPackageDataModel contract, string? existing)
    {
        return NpmPackageMapper.ToLocal(contract);
    }

    protected override StsNpmPackageDataModel NormalizeContract(StsNpmPackageDataModel contract)
    {
        return NpmPackageMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsNpmPackageDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetNpmPackages(cancellationToken);
    }

    protected override string GetKey(StsNpmPackageDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsNpmPackageDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsNpmPackageDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateNpmPackage(key, contract, cancellationToken);
    }
}
