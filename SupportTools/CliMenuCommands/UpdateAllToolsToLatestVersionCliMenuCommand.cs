using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibParameters;
using SupportTools.Tools;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateAllToolsToLatestVersionCliMenuCommand : CliMenuCommand
{
    private readonly Func<string, bool> _confirm;
    private readonly IParametersManager _parametersManager;

    public UpdateAllToolsToLatestVersionCliMenuCommand(IParametersManager parametersManager) : this(parametersManager,
        question => Inputer.InputBool(question, true, false))
    {
    }

    //კონსოლიდან დასტური პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal UpdateAllToolsToLatestVersionCliMenuCommand(IParametersManager parametersManager,
        Func<string, bool> confirm) : base("Update All Tools To Latest Version", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _confirm = confirm;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!_confirm("Are you sure, you want to Update All Tools To Latest Version?"))
        {
            return false;
        }

        //განახლება ხელსაწყოების ვერსიებს ცვლის (წარუმატებლობისასაც), ამიტომ შედეგი ყოველთვის ინახება
        bool updated = DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager);
        await _parametersManager.Save(_parametersManager.Parameters, "Dotnet Tools versions saved", null,
            cancellationToken);
        return updated;
    }
}
