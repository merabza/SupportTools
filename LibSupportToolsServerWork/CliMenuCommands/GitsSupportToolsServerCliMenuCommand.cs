using CliMenu;
using CliParameters.CliMenuCommands;
using LibGitData;
using SupportToolsData.Models;
using System.Net.Http;
using System;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibSupportToolsServerWork.CliMenuCommands;

internal class GitsSupportToolsServerCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsSupportToolsServerCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager) : base("Gits from SupportToolsServer", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }
    
    //public override CliMenuSet GetSubMenu(object record)
    //{
    //    var parameters = (SupportToolsParameters)_parametersManager.Parameters;

    //    var gitsFromServer = SupportToolsServerWork.GetGitRepos(_logger, _httpClientFactory, parameters);

    //    var gitCruder = GitCruder.Create(_logger, _httpClientFactory, _parametersManager);
    //    //ჩამოვტვირთოთ გიტ სერვერიდან ინფორმაცია ყველა გიტ პროექტების შესახებ და შემდეგ ეს ინფორმაცია გამოვიყენოთ სიის ჩვენებისას
    //    var menuSet = gitCruder.GetListMenu();
    //    return menuSet;
    //}



    //    public override CliMenuSet GetSubmenu()
    //{
    //    CliMenuSet gitSubMenuSet = new("GitProjects");

    //    var parameters = (SupportToolsParameters)_parametersManager.Parameters;

    //    //ყველას სინქრონიზაციის გაშვება
    //    //თითოული git ნაწილისათვის შემოწმდეს არის თუ არა რამე დასაკომიტებელი, თუ არის, დაკომიტდეს ავტომატური მესიჯის შემოთავაზებით და ახალი მესიჯის შეყვანის საშუალებით
    //    //მესიჯის შეყვანის შემდეგ გამოვიდეს შეკითხვა გავრცელდეს თუ არა იგივე მესიჯი სხვა დანარჩენ ცვლილებებზე თუ იქნება.
    //    //გიტის დაკომიტების მერე გაეშვას პული და ბოლოს პუში
    //    gitSubMenuSet.AddMenuItem(
    //        new SyncOneProjectAllGitsCliMenuCommand(_logger, _parametersManager, _projectName, _gitCol));

    //    if (_gitCol == EGitCol.Main) //მხოლოდ მთავარი ვარიანტისთვის არის შესაძლებელი ახალი გიტის დამატება ამოკლება
    //    {
    //        //გიტების ინფორმაციის ჩატვირთვა კლონირების ფაილიდან
    //        gitSubMenuSet.AddMenuItem(new LoadGitsFromCloneFileCommand(_parametersManager, _projectName));

    //        //გიტების ინფორმაციაზე დაყრდნობით კლონირების ფაილის შექმნა
    //        gitSubMenuSet.AddMenuItem(new SaveGitsCloneFileCliMenuCommand(_parametersManager, _projectName));
    //        //მომავალში სასურველი იქნება ბრენჩების შექმნის ათვისება და pull Request-ების ათვისება.
    //    }

    //    //მენიუს ელემენტი, რომლის საშუალებითაც შესაძლებელია პროექტში გიტის ჩაგდება
    //    NewGitCliMenuCommand newGitCommand = new(_logger, _httpClientFactory, _parametersManager, _projectName, _gitCol);
    //    gitSubMenuSet.AddMenuItem(newGitCommand);

    //    //იმ გიტების ჩამონათვალი, რომლებიც ამ პროექტში შედიან
    //    //თითოეულზე უნდა შეიძლებოდეს ქვემენიუში შესვლა, რომელიც საშუალებას მოგვცემს გიტის ეს კონკრეტული ნაწილი ამოვშალოთ პროექტიდან
    //    //ასევე შესაძელებელი უნდა იყოს გიტის დასინქრონიზება და ძირითადი ბრძანებების გაშვება
    //    //string gitsFolder = parameters.GetGitsFolder(_projectName, _gitCol);


    //    var gitProjectNames = parameters.GetGitProjectNames(_projectName, _gitCol);

    //    foreach (var gitProjectName in gitProjectNames.OrderBy(o => o))
    //        gitSubMenuSet.AddMenuItem(new GitProjectSubMenuCliMenuCommand(_logger, _parametersManager, _projectName,
    //            gitProjectName, _gitCol));

    //    //მთავარ მენიუში გასვლა
    //    var key = ConsoleKey.Escape.Value().ToLower();
    //    gitSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);

    //    return gitSubMenuSet;
    //}


}