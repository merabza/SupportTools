using System.Threading;
using System.Threading.Tasks;
using ParametersManagement.LibParameters;

namespace SupportTools.AutoSync;

//IParametersManager-ის decorator (D2): შენახვას შიდა მენეჯერს (SupportToolsMainParametersManager) აბარებს, წარმატებული
//შენახვის შემდეგ კი RegistryAutoSync-ით არაკონფლიქტურ Push-ებს აგზავნის. Parameters შიდა მენეჯერის იგივე ობიექტია,
//ამიტომ ~150 (SupportToolsParameters)parametersManager.Parameters cast უცვლელად მუშაობს. Save ყოველთვის ლოკალური
//შენახვის შედეგს აბრუნებს: სერვერის შეცდომა მას ვერ აფუჭებს. DI-ში ერთადერთი IParametersManager ეს არის
//(SupportToolsServices), ამიტომ მენიუც და --run-იც (ProjectToolRunner) მას იყენებს. ParametersManagement უცვლელია:
//AddMainParametersManager<T> T : ParametersManager-ს მოითხოვს, ამიტომ რეგისტრაცია ხელითაა
public sealed class AutoSyncParametersManager : IParametersManager
{
    private readonly RegistryAutoSync _autoSync;
    private readonly IParametersManager _localParametersManager;

    public AutoSyncParametersManager(SupportToolsMainParametersManager localParametersManager,
        RegistryAutoSync autoSync) : this((IParametersManager)localParametersManager, autoSync)
    {
    }

    //შიდა მენეჯერი ინტერფეისითაა, რომ ტესტებმა თავისი მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal AutoSyncParametersManager(IParametersManager localParametersManager, RegistryAutoSync autoSync)
    {
        _localParametersManager = localParametersManager;
        _autoSync = autoSync;
    }

    public IParameters Parameters
    {
        get => _localParametersManager.Parameters;
        set => _localParametersManager.Parameters = value;
    }

    public string? ParametersFileName => _localParametersManager.ParametersFileName;

    public async ValueTask<bool> Save(IParameters parameters, string message, string? saveAsFilePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _localParametersManager.Save(parameters, message, saveAsFilePath, cancellationToken))
        {
            return false;
        }

        await _autoSync.PushAfterSave(cancellationToken);
        return true;
    }

    //შენახვა ავტომატური სინქრონიზაციის გარეშე. ხელით სინქრონიზაცია (C5) თვითონ ირჩევს, რა გაიგზავნოს, და მდგომარეობას
    //თვითონ ინახავს: მისი შენახვის შემდეგ Push არ უნდა მოხდეს (მაგ. Pull only-ის შემდეგ ან ჩანაწერის გამორიცხვისას)
    public static IParametersManager WithoutAutoSync(IParametersManager parametersManager)
    {
        return parametersManager is AutoSyncParametersManager autoSyncParametersManager
            ? autoSyncParametersManager._localParametersManager
            : parametersManager;
    }
}
