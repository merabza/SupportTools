using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//React აპლიკაციების შაბლონები: SupportToolsParameters.ReactAppTemplates (სახელი → --template მნიშვნელობა) ↔
//StsReactAppTemplateDataModel (B2)
public sealed class ReactAppTemplatesRegistrySyncAdapter : DictionaryRegistrySyncAdapter<string,
    StsReactAppTemplateDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReactAppTemplatesRegistrySyncAdapter(SupportToolsServerApiClient apiClient,
        SupportToolsParameters parameters, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.ReactAppTemplates;
    public override int Order => RegistryCollections.ReactAppTemplatesOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteReactAppTemplate(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, string> GetLocalDictionary()
    {
        return _parameters.ReactAppTemplates;
    }

    protected override StsReactAppTemplateDataModel ToContract(string key, string local)
    {
        return ReactAppTemplateMapper.ToContract(key, local);
    }

    protected override string ToLocal(StsReactAppTemplateDataModel contract, string? existing)
    {
        return ReactAppTemplateMapper.ToLocal(contract);
    }

    protected override StsReactAppTemplateDataModel NormalizeContract(StsReactAppTemplateDataModel contract)
    {
        return ReactAppTemplateMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsReactAppTemplateDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetReactAppTemplates(cancellationToken);
    }

    protected override string GetKey(StsReactAppTemplateDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsReactAppTemplateDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsReactAppTemplateDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateReactAppTemplate(key, contract, cancellationToken);
    }
}
