using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibParameters;
using SupportTools.Tools;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateOneToolToLatestVersionCliMenuCommand : CliMenuCommand
{
    private readonly Func<string, bool> _confirm;
    private readonly IParametersManager _parametersManager;
    private readonly string _toolKey;

    public UpdateOneToolToLatestVersionCliMenuCommand(IParametersManager parametersManager, string toolKey) : this(
        parametersManager, toolKey, question => Inputer.InputBool(question, true, false))
    {
    }

    //კონსოლიდან დასტური პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal UpdateOneToolToLatestVersionCliMenuCommand(IParametersManager parametersManager, string toolKey,
        Func<string, bool> confirm) : base("Update Tool To Latest Version", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _toolKey = toolKey;
        _confirm = confirm;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!_confirm($"Are you sure, you want to Update {_toolKey} To Latest Version?"))
        {
            return false;
        }

        //განახლება ხელსაწყოს ვერსიებს ცვლის (წარუმატებლობისასაც), ამიტომ შედეგი ყოველთვის ინახება
        bool updated = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager, _toolKey);
        await _parametersManager.Save(_parametersManager.Parameters, "Dotnet Tools versions saved", null,
            cancellationToken);
        return updated;
    }
}
