using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.BaGetter;

public sealed class DeleteBaGetterPackageCliMenuCommand : CliMenuCommand
{
    private readonly BaGetterApiClient _apiClient;
    private readonly ILogger _logger;
    private readonly string _packageId;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteBaGetterPackageCliMenuCommand(ILogger logger, BaGetterApiClient apiClient, string packageId) : base(
        "Delete all versions and the entire package", EMenuAction.LevelUp, EMenuAction.Reload, packageId)
    {
        _logger = logger;
        _apiClient = apiClient;
        _packageId = packageId;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!Inputer.InputBool($"This will Delete all versions of package {_packageId} from server. are you sure?",
                false, false))
        {
            return false;
        }

        //ვერსიების სია სერვერიდან თავიდან იკითხება, რომ ზუსტად მიმდინარე მდგომარეობა წაიშალოს
        List<string>? versions = await _apiClient.GetVersions(_packageId, cancellationToken);
        if (versions is null)
        {
            return false;
        }

        if (versions.Count == 0)
        {
            StShared.WriteErrorLine($"No versions found for package {_packageId}", true, _logger);
            return false;
        }

        bool hadErrors = false;
        foreach (string version in versions)
        {
            StShared.ConsoleWriteInformationLine(_logger, true, $"Deleting package {_packageId} version {version}...");
            hadErrors |= !await _apiClient.DeleteVersion(_packageId, version, cancellationToken);
        }

        return !hadErrors;
    }
}
