using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//ფაილსაცავები: SupportToolsParameters.FileStorages ↔ StsFileStorageDataModel (B3). ლოკალური FileStoragePath
//PathMapper-ით გარდაიქმნება; მომხმარებელი და პაროლი საიდუმლოა და არსად იბეჭდება
public sealed class FileStoragesRegistrySyncAdapter : DictionaryRegistrySyncAdapter<FileStorageData,
    StsFileStorageDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;
    private readonly PathMapper _pathMapper;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FileStoragesRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        PathMapper pathMapper, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
        _pathMapper = pathMapper;
    }

    public override string CollectionName => RegistryCollections.FileStorages;
    public override int Order => RegistryCollections.FileStoragesOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteFileStorage(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, FileStorageData> GetLocalDictionary()
    {
        return _parameters.FileStorages;
    }

    protected override StsFileStorageDataModel ToContract(string key, FileStorageData local)
    {
        return FileStorageMapper.ToContract(key, local, _pathMapper);
    }

    protected override FileStorageData ToLocal(StsFileStorageDataModel contract, FileStorageData? existing)
    {
        FileStorageData fileStorage = existing ?? new FileStorageData();
        FileStorageMapper.ApplyToLocal(contract, fileStorage, _pathMapper);
        return fileStorage;
    }

    protected override StsFileStorageDataModel NormalizeContract(StsFileStorageDataModel contract)
    {
        return FileStorageMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsFileStorageDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetFileStorages(cancellationToken);
    }

    protected override string GetKey(StsFileStorageDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsFileStorageDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsFileStorageDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateFileStorage(key, contract, cancellationToken);
    }
}
