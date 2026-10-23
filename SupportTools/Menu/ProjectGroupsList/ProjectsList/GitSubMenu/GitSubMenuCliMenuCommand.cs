using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using LibGitWork.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;

public sealed class GitSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string projectName) : base("Git", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    public override CliMenuSet GetSubMenu()
    {
        var gitSubMenuSet = new CliMenuSet("Git Projects");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ყველას სინქრონიზაციის გაშვება
        //თითოული git ნაწილისათვის შემოწმდეს არის თუ არა რამე დასაკომიტებელი, თუ არის, დაკომიტდეს ავტომატური მესიჯის შემოთავაზებით და ახალი მესიჯის შეყვანის საშუალებით
        //მესიჯის შეყვანის შემდეგ გამოვიდეს შეკითხვა გავრცელდეს თუ არა იგივე მესიჯი სხვა დანარჩენ ცვლილებებზე თუ იქნება.
        //გიტის დაკომიტების მერე გაეშვას პული და ბოლოს პუში
        gitSubMenuSet.AddMenuItem(new SyncOneProjectAllGitsCliMenuCommand(_logger, _parametersManager, _projectName));

        //გიტების ინფორმაციის ჩატვირთვა კლონირების ფაილიდან
        gitSubMenuSet.AddMenuItem(new LoadGitsFromCloneFileCommand(_parametersManager, _projectName));

        //გიტების ინფორმაციაზე დაყრდნობით კლონირების ფაილის შექმნა
        gitSubMenuSet.AddMenuItem(new SaveGitsCloneFileCliMenuCommand(_parametersManager, _projectName));
        //მომავალში სასურველი იქნება ბრენჩების შექმნის ათვისება და pull Request-ების ათვისება.

        //მენიუს ელემენტი, რომლის საშუალებითაც შესაძლებელია პროექტში გიტის ჩაგდება
        var newGitCommand = new NewGitCliMenuCommand(_logger, _httpClientFactory, _parametersManager, _projectName);
        gitSubMenuSet.AddMenuItem(newGitCommand);

        //იმ გიტების ჩამონათვალი, რომლებიც ამ პროექტში შედიან
        //თითოეულზე უნდა შეიძლებოდეს ქვემენიუში შესვლა, რომელიც საშუალებას მოგვცემს გიტის ეს კონკრეტული ნაწილი ამოვშალოთ პროექტიდან
        //ასევე შესაძელებელი უნდა იყოს გიტის დასინქრონიზება და ძირითადი ბრძანებების გაშვება
        //string gitsFolder = parameters.GetGitsFolder(_projectName);

        List<string> gitProjectNames = parameters.GetGitProjectNames(_projectName);

        foreach (string gitProjectName in gitProjectNames.OrderBy(o => o))
        {
            gitSubMenuSet.AddMenuItem(new GitProjectSubMenuCliMenuCommand(_logger, _parametersManager, _projectName,
                gitProjectName));
        }

        //მთავარ მენიუში გასვლა
        gitSubMenuSet.AddEscapeCommand();

        return gitSubMenuSet;
    }
}
