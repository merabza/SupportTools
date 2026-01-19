using System;
using AppCliTools.CliMenu;
using LibGitWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

//შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
//შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერში Gits ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
//შემოწმდეს Gits ფოლდერში პროექტის ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას

//თუ ფოლდერი არსებობს, მაშინ დადგინდეს შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
// თუ ეს ასე არ არის, წაიშალოს ფოლდერი თავისი შიგთავსით

//შემოწმდეს Gits ფოლდერში თუ არსებობს ფოლდერი gitDataModel.GitProjectFolderName სახელით და თუ არ არსებობს,
//მოხდეს ამ Git პროექტის დაკლონვა შესაბამისი ფოლდერის სახელის მითითებით

//თუ ფოლდერი არსებობს, მოხდეს სტატუსის შემოწმება (იდეაში აქ ცვლილებები არ უნდა მომხდარიყო, მაგრამ მაინც)
//  თუ აღმოჩნდა რომ ცვლილებები მომხდარა, გამოვიდეს შეტყობინება ცვლილებების გაუქმებისა და თავიდან დაკლონვის შესახებ
//     თანხმობის შემთხვევაში ან თავიდან დაიკლონოს, ან უკეთესია, თუ Checkout გაკეთდება.
//  თუ ცვლილებები არ მომხდარა, მოხდეს უბრალოდ Pull

public sealed class UpdateGitProjectCliMenuCommand : CliMenuCommand
{
    private readonly string _gitName;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly SupportToolsParameters _supportToolsParameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitProjectCliMenuCommand(ILogger logger, string gitName, IParametersManager parametersManager) : base(
        "Update Git Project", EMenuAction.LevelUp)
    {
        _logger = logger;
        _gitName = gitName;
        _supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        ////https://stackoverflow.com/questions/7293008/display-last-git-commit-comment
        ////https://unix.stackexchange.com/questions/196952/get-last-commit-message-author-and-hash-using-git-ls-remote-like-command            

        //            //ჩამოტვირთული გიტის ფაილები გაანალიზდეს, იმისათვის, რომ დადგინდეს, რა პროექტები არის ამ ფაილებში
        //            //და რომელ პროექტებზეა დამოკიდებული ეს პროექტები
        //            //დადგენილი ინფორმაციის შენახვა მოხდეს პარამეტრებში

        var gitProjectsUpdater = GitProjectsUpdater.Create(_logger, _parametersManager, _gitName, true);
        if (gitProjectsUpdater is null)
        {
            StShared.WriteErrorLine("gitProjectsUpdater does not created", true, _logger);
            return false;
        }

        var gitProcessor = gitProjectsUpdater.ProcessOneGitProject();
        if (gitProcessor is null)
            return false;

        _parametersManager.Save(_supportToolsParameters, "Project Saved");

        Console.WriteLine("Success");
        return true;
    }
}