using System;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibGitData;
using LibGitWork.CliMenuCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class FrontNpmPackageNamesSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FrontNpmPackageNamesSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager, string projectName) : base("Front Npm Package Names",
        EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    public override CliMenuSet GetSubMenu()
    {
        CliMenuSet gitSubMenuSet = new("Front Npm Package Names");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        ////მენიუს ელემენტი, რომლის საშუალებითაც შესაძლებელია პროექტში გიტის ჩაგდება
        //NewFrontNpmPackageNameCliMenuCommand newGitCommand = new(_logger, _httpClientFactory, _parametersManager, _projectName, _gitCol);
        //gitSubMenuSet.AddMenuItem(newGitCommand);

        ////იმ გიტების ჩამონათვალი, რომლებიც ამ პროექტში შედიან
        ////თითოეულზე უნდა შეიძლებოდეს ქვემენიუში შესვლა, რომელიც საშუალებას მოგვცემს გიტის ეს კონკრეტული ნაწილი ამოვშალოთ პროექტიდან
        ////ასევე შესაძელებელი უნდა იყოს გიტის დასინქრონიზება და ძირითადი ბრძანებების გაშვება
        ////string gitsFolder = parameters.GetGitsFolder(_projectName, _gitCol);


        //var gitProjectNames = parameters.GetGitProjectNames(_projectName, _gitCol);

        //foreach (var gitProjectName in gitProjectNames.OrderBy(o => o))
        //    gitSubMenuSet.AddMenuItem(new FrontNpmPackageNameSubMenuCliMenuCommand(_logger, _parametersManager, _projectName,
        //        gitProjectName, _gitCol));

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        gitSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);

        return gitSubMenuSet;
    }
}