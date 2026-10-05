using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//.gitignore შაბლონები: SupportToolsParameters.GitIgnorePatterns და {FolderForGitignoreFiles}\{name}.gitignore ფაილები ↔
//StsGitIgnoreFileTypeDataModel. სერვერის Id კლიენტს არ სჭირდება (სერვერი ჩანაწერებს სახელით ადარებს), ამიტომ ჰეშში
//არ შედის
public sealed class GitIgnorePatternsRegistrySyncAdapter : TemplateFilesRegistrySyncAdapter<
    StsGitIgnoreFileTypeDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnorePatternsRegistrySyncAdapter(SupportToolsServerApiClient apiClient,
        SupportToolsParameters parameters, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.GitIgnorePatterns;
    public override int Order => RegistryCollections.GitIgnorePatternsOrder;
    protected override string FolderFieldName => nameof(SupportToolsParameters.FolderForGitignoreFiles);

    protected override string ServerNotFoundErrorCode =>
        nameof(SupportToolsServerApiClientErrors.GitIgnoreFileTypeWithNameNotFound);

    protected override string? GetFolder()
    {
        return _parameters.FolderForGitignoreFiles;
    }

    protected override List<string> GetNames()
    {
        return _parameters.GitIgnorePatterns;
    }

    protected override string GetFilePath(string folder, string name)
    {
        return SupportToolsParameters.GetGitIgnoreModelFilePath(folder, name);
    }

    protected override StsGitIgnoreFileTypeDataModel CreateContract(string name, string content)
    {
        return new StsGitIgnoreFileTypeDataModel { Name = name, Content = content };
    }

    protected override string GetContent(StsGitIgnoreFileTypeDataModel contract)
    {
        return contract.Content;
    }

    protected override StsGitIgnoreFileTypeDataModel NormalizeContract(StsGitIgnoreFileTypeDataModel contract)
    {
        contract.Id = Guid.Empty;
        return contract;
    }

    protected override Task<Result<List<StsGitIgnoreFileTypeDataModel>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetGitIgnoreFileTypesList(cancellationToken);
    }

    protected override string GetKey(StsGitIgnoreFileTypeDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsGitIgnoreFileTypeDataModel contract)
    {
        return contract.Version;
    }

    //merge=true: სერვერი მხოლოდ ამ ჩანაწერს ამატებს ან ანაცვლებს და სხვას არ შლის
    protected override async Task<Result> WriteServerContract(StsGitIgnoreFileTypeDataModel contract,
        CancellationToken cancellationToken)
    {
        return await _apiClient.SyncUpGitIgnoreFileTypes([contract], true, cancellationToken);
    }

    protected override Task<Result> DeleteServerRecord(string key, CancellationToken cancellationToken)
    {
        return _apiClient.RemoveGitIgnoreFileTypeName(key, cancellationToken);
    }
}
