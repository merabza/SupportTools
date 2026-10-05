using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//.editorconfig შაბლონები: SupportToolsParameters.EditorConfigPatterns და
//{FolderForEditorConfigFiles}\{name}.editorconfig ფაილები ↔ StsEditorConfigFileTypeDataModel
public sealed class EditorConfigPatternsRegistrySyncAdapter : TemplateFilesRegistrySyncAdapter<
    StsEditorConfigFileTypeDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditorConfigPatternsRegistrySyncAdapter(SupportToolsServerApiClient apiClient,
        SupportToolsParameters parameters, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.EditorConfigPatterns;
    public override int Order => RegistryCollections.EditorConfigPatternsOrder;
    protected override string FolderFieldName => nameof(SupportToolsParameters.FolderForEditorConfigFiles);

    protected override string ServerNotFoundErrorCode =>
        nameof(SupportToolsServerApiClientErrors.EditorConfigFileTypeWithNameNotFound);

    protected override string? GetFolder()
    {
        return _parameters.FolderForEditorConfigFiles;
    }

    protected override List<string> GetNames()
    {
        return _parameters.EditorConfigPatterns;
    }

    protected override string GetFilePath(string folder, string name)
    {
        return SupportToolsParameters.GetEditorConfigPatternFilePath(folder, name);
    }

    protected override StsEditorConfigFileTypeDataModel CreateContract(string name, string content)
    {
        return new StsEditorConfigFileTypeDataModel { Name = name, Content = content };
    }

    protected override string GetContent(StsEditorConfigFileTypeDataModel contract)
    {
        return contract.Content;
    }

    //შიგთავსი ფაილისაა და უცვლელი გადაიცემა
    protected override StsEditorConfigFileTypeDataModel NormalizeContract(StsEditorConfigFileTypeDataModel contract)
    {
        return contract;
    }

    protected override Task<Result<List<StsEditorConfigFileTypeDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetEditorConfigFileTypesList(cancellationToken);
    }

    protected override string GetKey(StsEditorConfigFileTypeDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsEditorConfigFileTypeDataModel contract)
    {
        return contract.Version;
    }

    //merge=true: სერვერი მხოლოდ ამ ჩანაწერს ამატებს ან ანაცვლებს და სხვას არ შლის
    protected override async Task<Result> WriteServerContract(StsEditorConfigFileTypeDataModel contract,
        CancellationToken cancellationToken)
    {
        return await _apiClient.SyncUpEditorConfigFileTypes([contract], true, cancellationToken);
    }

    protected override Task<Result> DeleteServerRecord(string key, CancellationToken cancellationToken)
    {
        return _apiClient.RemoveEditorConfigFileTypeName(key, cancellationToken);
    }
}
