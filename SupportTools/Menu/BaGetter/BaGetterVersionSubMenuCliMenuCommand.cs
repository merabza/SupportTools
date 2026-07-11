using AppCliTools.CliMenu;

namespace SupportTools.Menu.BaGetter;

public sealed class BaGetterVersionSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly BaGetterApiClient _apiClient;
    private readonly string _packageId;
    private readonly string _version;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BaGetterVersionSubMenuCliMenuCommand(BaGetterApiClient apiClient, string packageId, string version) : base(
        version, EMenuAction.LoadSubMenu)
    {
        _apiClient = apiClient;
        _packageId = packageId;
        _version = version;
    }

    public override CliMenuSet GetSubMenu()
    {
        var versionMenuSet = new CliMenuSet($"{_packageId} {_version}");

        //ამ ვერსიის წაშლა სერვერიდან
        versionMenuSet.AddMenuItem(new DeleteBaGetterPackageVersionCliMenuCommand(_apiClient, _packageId, _version));

        versionMenuSet.AddEscapeCommand();
        return versionMenuSet;
    }
}
