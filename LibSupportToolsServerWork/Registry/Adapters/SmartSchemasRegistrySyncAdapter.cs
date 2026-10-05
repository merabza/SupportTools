using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//ჭკვიანი სქემები: SupportToolsParameters.SmartSchemas ↔ StsSmartSchemaDataModel (B3), დეტალებით. SmartSchema-ს
//თვისებები init-only-ა, ამიტომ ApplyLocal ჩანაწერს ახალი ეგზემპლარით ცვლის; კომპიუტერის ველები მას არ აქვს
public sealed class SmartSchemasRegistrySyncAdapter : DictionaryRegistrySyncAdapter<SmartSchema,
    StsSmartSchemaDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SmartSchemasRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.SmartSchemas;
    public override int Order => RegistryCollections.SmartSchemasOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteSmartSchema(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, SmartSchema> GetLocalDictionary()
    {
        return _parameters.SmartSchemas;
    }

    protected override StsSmartSchemaDataModel ToContract(string key, SmartSchema local)
    {
        return SmartSchemaMapper.ToContract(key, local);
    }

    protected override SmartSchema ToLocal(StsSmartSchemaDataModel contract, SmartSchema? existing)
    {
        return SmartSchemaMapper.ToLocal(contract);
    }

    protected override StsSmartSchemaDataModel NormalizeContract(StsSmartSchemaDataModel contract)
    {
        return SmartSchemaMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsSmartSchemaDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetSmartSchemas(cancellationToken);
    }

    protected override string GetKey(StsSmartSchemaDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsSmartSchemaDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsSmartSchemaDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateSmartSchema(key, contract, cancellationToken);
    }

    protected override string? FindUnsupportedField(StsSmartSchemaDataModel contract)
    {
        return SmartSchemaMapper.HasUnknownPeriodType(contract)
            ? $"{nameof(StsSmartSchemaDataModel.Details)}.{nameof(StsSmartSchemaDetailDataModel.PeriodType)}"
            : null;
    }
}
