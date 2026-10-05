using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//git რეპოზიტორიები: SupportToolsParameters.Gits ↔ StsGitDataModel. სერვერის endpoint-ები (updategitrepo/{key},
//deletegitrepo/{key}) ძველია და ვერსიას არ ამოწმებს: იხ. UpsertCheckingVersionOnClient. სერვერზე git-ის დამატება ან
//შეცვლა იქ git clone/pull-ს უშვებს
public sealed class GitsRegistrySyncAdapter : DictionaryRegistrySyncAdapter<GitDataModel, StsGitDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;
    private readonly PathMapper _pathMapper;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        PathMapper pathMapper, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
        _pathMapper = pathMapper;
    }

    public override string CollectionName => RegistryCollections.Gits;
    public override int Order => RegistryCollections.GitsOrder;

    public override Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return DeleteCheckingVersionOnClient(key, expectedVersion,
            () => _apiClient.RemoveGitRepoByKey(key, cancellationToken),
            nameof(SupportToolsServerApiClientErrors.GitWithKeyNotFound), cancellationToken);
    }

    protected override Dictionary<string, GitDataModel> GetLocalDictionary()
    {
        return _parameters.Gits;
    }

    protected override StsGitDataModel ToContract(string key, GitDataModel local)
    {
        return GitMapper.ToContract(key, local, _pathMapper);
    }

    protected override GitDataModel ToLocal(StsGitDataModel contract, GitDataModel? existing)
    {
        GitDataModel git = existing ?? new GitDataModel();
        GitMapper.ApplyToLocal(contract, git, _pathMapper);
        return git;
    }

    protected override StsGitDataModel NormalizeContract(StsGitDataModel contract)
    {
        return GitMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsGitDataModel>>> GetServerContracts(CancellationToken cancellationToken)
    {
        return _apiClient.GetGitRepos(cancellationToken);
    }

    protected override string GetKey(StsGitDataModel contract)
    {
        return contract.GitProjectName;
    }

    protected override int GetVersion(StsGitDataModel contract)
    {
        return contract.Version;
    }

    //route-ის key იმარჯვებს კონტრაქტის GitProjectName-ზე
    protected override Task<Result<int>> UpsertContract(string key, StsGitDataModel contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        return UpsertCheckingVersionOnClient(key, expectedVersion,
            () => _apiClient.UpdateGitRepoByKey(key, contract, cancellationToken), cancellationToken);
    }
}
