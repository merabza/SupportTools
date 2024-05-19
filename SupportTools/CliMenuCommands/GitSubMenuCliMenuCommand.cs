﻿using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibGitData;
using LibGitWork.CliMenuCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class GitSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;

    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSubMenuCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        EGitCol gitCol) : base(projectName, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitCol = gitCol;
    }

    public override CliMenuSet GetSubmenu()
    {
        CliMenuSet gitSubMenuSet = new("GitProjects");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ყველას სინქრონიზაციის გაშვება
        //თითოული git ნაწილისათვის შემოწმდეს არის თუ არა რამე დასაკომიტებელი, თუ არის, დაკომიტდეს ავტომატური მესიჯის შემოთავაზებით და ახალი მესიჯის შეყვანის საშუალებით
        //მესიჯის შეყვანის შემდეგ გამოვიდეს შეკითხვა გავრცელდეს თუ არა იგივე მესიჯი სხვა დანარჩენ ცვლილებებზე თუ იქნება.
        //გიტის დაკომიტების მერე გაეშვას პული და ბოლოს პუში
        gitSubMenuSet.AddMenuItem(
            new SyncOneProjectAllGitsCliMenuCommand(_logger, _parametersManager, _projectName, _gitCol));

        if (_gitCol == EGitCol.Main) //მხოლოდ მთავარი ვარიანტისთვის არის შესაძლებელი ახალი გიტის დამატება ამოკლება
        {
            //გიტების ინფორმაციის ჩატვირთვა კლონირების ფაილიდან
            gitSubMenuSet.AddMenuItem(new LoadGitsFromCloneFileCommand(_parametersManager, _projectName));

            //გიტების ინფორმაციაზე დაყრდნობით კლონირების ფაილის შექმნა
            gitSubMenuSet.AddMenuItem(new SaveGitsCloneFileCliMenuCommand(_logger, _parametersManager, _projectName));
            //მომავალში სასურველი იქნება ბრენჩების შექმნის ათვისება და pull Request-ების ათვისება.
        }

        //მენიუს ელემენტი, რომლის საშუალებითაც შესაძლებელია პროექტში გიტის ჩაგდება
        NewGitCliMenuCommand newGitCommand = new(_logger, _parametersManager, _projectName, _gitCol);
        gitSubMenuSet.AddMenuItem(newGitCommand);

        //იმ გიტების ჩამონათვალი, რომლებიც ამ პროექტში შედიან
        //თითოეულზე უნდა შეიძლებოდეს ქვემენიუში შესვლა, რომელიც საშუალებას მოგვცემს გიტის ეს კონკრეტული ნაწილი ამოვშალოთ პროექტიდან
        //ასევე შესაძელებელი უნდა იყოს გიტის დასინქრონიზება და ძირითადი ბრძანებების გაშვება
        //string gitsFolder = parameters.GetGitsFolder(_projectName, _gitCol);


        var result = parameters.GetGitProjectNames(_projectName, _gitCol);
        if (result.IsNone)
        {
            StShared.WriteErrorLine(
                $"Git Project with name {_projectName} does not exists", true);
            return gitSubMenuSet;
        }

        var gitProjectNames = (List<string>)result;
        foreach (var gitProjectName in gitProjectNames.OrderBy(o => o))
            gitSubMenuSet.AddMenuItem(
                new GitProjectSubMenuCliMenuCommand(_logger, _parametersManager, _projectName, gitProjectName, _gitCol),
                gitProjectName);

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        gitSubMenuSet.AddMenuItem(key, "Exit to Main menu", new ExitToMainMenuCliMenuCommand(null, null), key.Length);

        return gitSubMenuSet;
    }
}