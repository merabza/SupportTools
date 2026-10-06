using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//პროექტის შემქმნელის პარამეტრები (B5 singleton): AppProjectCreatorAllParameters, Templates-ის გარეშე ↔
//StsProjectCreatorSettingsDataModel. ProjectsFolderPathReal და SecretsFolderPathReal PathMapper-ით გარდაიქმნება.
//თუ AppProjectCreatorAllParameters ჯერ არ არსებობს, ApplyLocal მას ქმნის
public sealed class ProjectCreatorSettingsRegistrySyncAdapter : SingletonRegistrySyncAdapter<
    StsProjectCreatorSettingsDataModel>
{
    //ჩანაწერის გასაღები: სერვერის RecordName (ProjectCreatorSettingsContractMapper)
    public const string RecordKey = "ProjectCreator";

    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;
    private readonly PathMapper _pathMapper;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectCreatorSettingsRegistrySyncAdapter(SupportToolsServerApiClient apiClient,
        SupportToolsParameters parameters, PathMapper pathMapper, RegistrySyncWarnings warnings) : base(RecordKey,
        warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
        _pathMapper = pathMapper;
    }

    public override string CollectionName => RegistryCollections.ProjectCreatorSettings;
    public override int Order => RegistryCollections.ProjectCreatorSettingsOrder;

    protected override StsProjectCreatorSettingsDataModel GetLocalContract()
    {
        return ProjectCreatorSettingsMapper.ToContract(_parameters.AppProjectCreatorAllParameters, _pathMapper);
    }

    protected override void ApplyContract(string key, StsProjectCreatorSettingsDataModel contract)
    {
        _parameters.AppProjectCreatorAllParameters ??= new AppProjectCreatorAllParameters();
        ProjectCreatorSettingsMapper.ApplyToLocal(contract, _parameters.AppProjectCreatorAllParameters, _pathMapper);
    }

    protected override StsProjectCreatorSettingsDataModel NormalizeContract(StsProjectCreatorSettingsDataModel contract)
    {
        return ProjectCreatorSettingsMapper.Normalize(contract);
    }

    protected override Task<Result<StsProjectCreatorSettingsDataModel>> GetServerContract(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetProjectCreatorSettings(cancellationToken);
    }

    protected override int GetVersion(StsProjectCreatorSettingsDataModel contract)
    {
        return contract.Version;
    }

    protected override Task<Result<int>> WriteServerContract(StsProjectCreatorSettingsDataModel contract, int version,
        CancellationToken cancellationToken)
    {
        contract.Version = version;
        return _apiClient.UpdateProjectCreatorSettings(contract, cancellationToken);
    }
}
