using System;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibGitWork.CliMenuCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectGroupSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string projectGroupName) : base(projectGroupName, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
    }

    public override CliMenuSet GetSubmenu()
    {
        var projectGroupSubMenuSet = new CliMenuSet($"Projects Group => {_projectGroupName}");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ყველა პროექტის git-ის სინქრონიზაცია
        var syncGroupAllProjectsGits =
            new SyncOneGroupAllProjectsGitsCliMenuCommand(_logger, _parametersManager, _projectGroupName);
        projectGroupSubMenuSet.AddMenuItem(syncGroupAllProjectsGits);

        //პროექტების ჩამონათვალი
        foreach (var (projectName, _) in parameters.Projects
                     .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                                 _projectGroupName).OrderBy(o => o.Key))
            //projectName
            projectGroupSubMenuSet.AddMenuItem(new ProjectSubMenuCliMenuCommand(_logger, _httpClientFactory,
                _parametersManager, projectName));
        
        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        projectGroupSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null),
            key.Length);

        return projectGroupSubMenuSet;
    }

    protected override string GetStatus()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        return parameters.Projects
            .Count(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
            .ToString();
    }
}