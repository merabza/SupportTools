using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//გლობალური პარამეტრები (B5 singleton): SupportToolsParameters-ის საერთო ზედა დონის ველები და
//DatabasesBackupFilesExchangeParameters (LocalPath-ის გარეშე) ↔ StsGlobalSettingsDataModel. MediatRLicenseKey
//საიდუმლოა და არსად იბეჭდება
public sealed class GlobalSettingsRegistrySyncAdapter : SingletonRegistrySyncAdapter<StsGlobalSettingsDataModel>
{
    //ჩანაწერის გასაღები: სერვერის RecordName (GlobalSettingsContractMapper)
    public const string RecordKey = "Global";

    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GlobalSettingsRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        RegistrySyncWarnings warnings) : base(RecordKey, warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
    }

    public override string CollectionName => RegistryCollections.GlobalSettings;
    public override int Order => RegistryCollections.GlobalSettingsOrder;

    protected override StsGlobalSettingsDataModel GetLocalContract()
    {
        return GlobalSettingsMapper.ToContract(_parameters);
    }

    protected override void ApplyContract(string key, StsGlobalSettingsDataModel contract)
    {
        GlobalSettingsMapper.ApplyToLocal(contract, _parameters);
    }

    protected override StsGlobalSettingsDataModel NormalizeContract(StsGlobalSettingsDataModel contract)
    {
        return GlobalSettingsMapper.Normalize(contract);
    }

    protected override Task<Result<StsGlobalSettingsDataModel>> GetServerContract(
        CancellationToken cancellationToken)
    {
        return _apiClient.GetGlobalSettings(cancellationToken);
    }

    protected override int GetVersion(StsGlobalSettingsDataModel contract)
    {
        return contract.Version;
    }

    protected override Task<Result<int>> WriteServerContract(StsGlobalSettingsDataModel contract, int version,
        CancellationToken cancellationToken)
    {
        contract.Version = version;
        return _apiClient.UpdateGlobalSettings(contract, cancellationToken);
    }
}
