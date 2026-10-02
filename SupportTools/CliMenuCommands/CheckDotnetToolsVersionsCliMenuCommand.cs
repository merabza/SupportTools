using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportTools.Tools;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class CheckDotnetToolsVersionsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckDotnetToolsVersionsCliMenuCommand(IParametersManager parametersManager) : base(
        "Check Dotnet Tools Versions...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //შემოწმება ხელსაწყოების ვერსიებს ცვლის, ამიტომ შედეგი ყოველთვის ინახება: ერთი ხელსაწყოს შეცდომისას Check
        //false-ს აბრუნებს, დანარჩენების ვერსიები კი შეიძლება უკვე შეცვლილი იყოს. უცვლელ ფაილს ParametersManager
        //თავიდან აღარ წერს
        DotnetToolsVersionsCheckerUpdater.Check(_parametersManager);
        await _parametersManager.Save(parameters, "Dotnet Tools versions saved", null, cancellationToken);

        return true;
    }
}
