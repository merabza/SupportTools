using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;

namespace SupportTools.Menu.BaGetter;

public sealed class DeleteBaGetterPackageVersionCliMenuCommand : CliMenuCommand
{
    private readonly BaGetterApiClient _apiClient;
    private readonly string _packageId;
    private readonly string _version;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteBaGetterPackageVersionCliMenuCommand(BaGetterApiClient apiClient, string packageId, string version) :
        base("Delete", EMenuAction.LevelUp, EMenuAction.Reload, version)
    {
        _apiClient = apiClient;
        _packageId = packageId;
        _version = version;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!Inputer.InputBool(
                $"This will Delete version {_version} of package {_packageId} from server. are you sure?", false,
                false))
        {
            return false;
        }

        //წარმატების შემთხვევაში LevelUp აბრუნებს ვერსიების სიაში, რომელიც თავიდან იკითხება სერვერიდან
        return await _apiClient.DeleteVersion(_packageId, _version, cancellationToken);
    }
}
