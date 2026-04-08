using System.Globalization;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using LibGitWork.CliMenuCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectGroupSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectGroupName;
    private readonly string _appName;
    private readonly ServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupSubMenuCliMenuCommand(string appName, ServiceProvider serviceProvider, ILogger logger,
        IHttpClientFactory httpClientFactory, ParametersManager parametersManager, string projectGroupName) : base(
        projectGroupName, EMenuAction.LoadSubMenu)
    {
        _appName = appName;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
    }

    public override CliMenuSet GetSubMenu()
    {
        var projectGroupSubMenuSet = new CliMenuSet(_projectGroupName);

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        ////ყველა პროექტის git-ის სინქრონიზაცია
        //var syncGroupAllProjectsGits =
        //    new SyncOneGroupAllProjectsGitsCliMenuCommand(_logger, _parametersManager, _projectGroupName);
        //projectGroupSubMenuSet.AddMenuItem(syncGroupAllProjectsGits);

        //ყველა პროექტის git-ის სინქრონიზაცია V2
        var syncGroupAllProjectsGitsV2 =
            new SyncOneGroupAllProjectsGitsCliMenuCommandV2(_logger, _parametersManager, _projectGroupName);
        projectGroupSubMenuSet.AddMenuItem(syncGroupAllProjectsGitsV2);

        //პროექტების ჩამონათვალი
        foreach ((string projectName, ProjectModel _) in parameters.Projects
                     .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                                 _projectGroupName).OrderBy(o => o.Key))
            //projectName
        {
            projectGroupSubMenuSet.AddMenuItem(new ProjectSubMenuCliMenuCommand(_appName, _serviceProvider, _logger,
                _httpClientFactory, _parametersManager, projectName));
        }

        //მთავარ მენიუში გასვლა
        projectGroupSubMenuSet.AddEscapeCommand();

        return projectGroupSubMenuSet;
    }

    protected override string GetStatus()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        return parameters.Projects.Count(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
            .ToString(CultureInfo.InvariantCulture);
    }
}
