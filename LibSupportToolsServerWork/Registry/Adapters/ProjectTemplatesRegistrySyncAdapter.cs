using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//პროექტის შაბლონები: AppProjectCreatorAllParameters.Templates ↔ StsProjectTemplateDataModel (B5). შაბლონები
//AppProjectCreatorAllParameters-შია, რომელიც ახალ კომპიუტერზე შეიძლება ჯერ არ არსებობდეს: ApplyLocal მას ქმნის
public sealed class ProjectTemplatesRegistrySyncAdapter : DictionaryRegistrySyncAdapter<TemplateModel,
    StsProjectTemplateDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectTemplatesRegistrySyncAdapter(SupportToolsServerApiClient apiClient,
        SupportToolsParameters parameters, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.ProjectTemplates;
    public override int Order => RegistryCollections.ProjectTemplatesOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteProjectTemplate(key, expectedVersion, cancellationToken);
    }

    protected override void ApplyContract(string key, StsProjectTemplateDataModel contract)
    {
        _parameters.AppProjectCreatorAllParameters ??= new AppProjectCreatorAllParameters();
        base.ApplyContract(key, contract);
    }

    //AppProjectCreatorAllParameters-ის გარეშე შაბლონები არ არის: ცარიელი dictionary მასში არ ჩაიწერება
    protected override Dictionary<string, TemplateModel> GetLocalDictionary()
    {
        return _parameters.AppProjectCreatorAllParameters?.Templates ?? [];
    }

    protected override StsProjectTemplateDataModel ToContract(string key, TemplateModel local)
    {
        return ProjectTemplateMapper.ToContract(key, local);
    }

    protected override TemplateModel ToLocal(StsProjectTemplateDataModel contract, TemplateModel? existing)
    {
        TemplateModel template = existing ?? new TemplateModel();
        ProjectTemplateMapper.ApplyToLocal(contract, template);
        return template;
    }

    protected override StsProjectTemplateDataModel NormalizeContract(StsProjectTemplateDataModel contract)
    {
        return ProjectTemplateMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsProjectTemplateDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetProjectTemplates(cancellationToken);
    }

    protected override string GetKey(StsProjectTemplateDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsProjectTemplateDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1)
    protected override Task<Result<int>> UpsertContract(string key, StsProjectTemplateDataModel contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        contract.Version = expectedVersion;
        return _apiClient.UpdateProjectTemplate(key, contract, cancellationToken);
    }

    protected override string? FindUnsupportedField(StsProjectTemplateDataModel contract)
    {
        return ProjectTemplateMapper.HasUnknownSupportProjectType(contract)
            ? nameof(StsProjectTemplateDataModel.SupportProjectType)
            : null;
    }
}
