using System.Collections.Generic;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;

namespace SupportTools.Menu.BaGetter;

public sealed class BaGetterPackageSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly BaGetterApiClient _apiClient;
    private readonly ILogger _logger;
    private readonly string _packageId;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BaGetterPackageSubMenuCliMenuCommand(ILogger logger, BaGetterApiClient apiClient, string packageId) : base(
        packageId, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _apiClient = apiClient;
        _packageId = packageId;
    }

    public override CliMenuSet GetSubMenu()
    {
        var packageMenuSet = new CliMenuSet(_packageId);

        //ვერსიების სია ყოველ ჯერზე თავიდან იკითხება სერვერიდან,
        //რომ წაშლის შემდეგ მენიუში განახლებული მდგომარეობა გამოჩნდეს
        List<string>? versions = _apiClient.GetVersions(_packageId).Result;
        if (versions is null)
        {
            packageMenuSet.AddEscapeCommand();
            return packageMenuSet;
        }

        //ყველა ვერსიის და მთლიანად პაკეტის წაშლა
        packageMenuSet.AddMenuItem(new DeleteBaGetterPackageCliMenuCommand(_logger, _apiClient, _packageId));

        //პაკეტის ვერსიების ჩამონათვალი
        foreach (string version in versions)
        {
            packageMenuSet.AddMenuItem(new BaGetterVersionSubMenuCliMenuCommand(_apiClient, _packageId, version));
        }

        packageMenuSet.AddEscapeCommand();
        return packageMenuSet;
    }
}
