using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using LibGitData;
using LibGitWork.CliMenuCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;
    private readonly ServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSubMenuCliMenuCommand(string appName, ServiceProvider serviceProvider, ILogger logger,
        IHttpClientFactory httpClientFactory, ParametersManager parametersManager, string projectName) : base(
        projectName, EMenuAction.LoadSubMenu)
    {
        _appName = appName;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    public override CliMenuSet GetSubMenu()
    {
        var projectSubMenuSet = new CliMenuSet(_projectName);

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //პროექტის წაშლა
        var deleteProjectCommand = new DeleteProjectCliMenuCommand(_parametersManager, _projectName);
        projectSubMenuSet.AddMenuItem(deleteProjectCommand);

        //პროექტის ექსპორტი
        var exportProjectCommand = new ExportProjectCliMenuCommand(_parametersManager, _projectName);
        projectSubMenuSet.AddMenuItem(exportProjectCommand);

        //პროექტის პარამეტრი
        var projectCruder = ProjectCruder.Create(_appName, _logger, _httpClientFactory, _parametersManager);
        var editCommand = new EditItemAllFieldsInSequenceCliMenuCommand(projectCruder, _projectName);
        projectSubMenuSet.AddMenuItem(editCommand);

        projectCruder.FillDetailsSubMenu(projectSubMenuSet, _projectName);

        ////ყველა პროექტის git-ის სინქრონიზაცია
        //var syncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand =
        //    new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand(_logger, _parametersManager, _projectName);
        //projectSubMenuSet.AddMenuItem(syncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand);

        //ყველა პროექტის git-ის სინქრონიზაცია V2
        var syncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2 =
            new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2(_logger, _parametersManager, _projectName);
        projectSubMenuSet.AddMenuItem(syncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2);

        projectSubMenuSet.AddMenuItem(new GitSubMenuCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _projectName, EGitCol.Main));

        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is not null)
        {
            if (!string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
            {
                projectSubMenuSet.AddMenuItem(new GitSubMenuCliMenuCommand(_logger, _httpClientFactory,
                    _parametersManager, _projectName, EGitCol.ScaffoldSeed));
            }

            //დასაშვები ინსტრუმენტების არჩევა
            projectSubMenuSet.AddMenuItem(new SelectProjectAllowToolsCliMenuCommand(_parametersManager, _projectName));

            foreach (EProjectTools tool in Enum.GetValues<EProjectTools>().Intersect(project.AllowToolsList)
                         .OrderBy(x => x.GetProjectToolName()))
            {
                projectSubMenuSet.AddMenuItem(new ProjectToolTaskCliMenuCommand(_serviceProvider, tool, _projectName,
                    _parametersManager));
            }
        }

        var serverInfoCruder = ServerInfoCruder.Create(_appName, _serviceProvider, _logger, _httpClientFactory,
            _parametersManager, _projectName);

        //ახალი სერვერის ინფორმაციის შექმნა
        var newItemCommand = new NewItemCliMenuCommand(serverInfoCruder, serverInfoCruder.CrudNamePlural,
            $"New {serverInfoCruder.CrudName}");
        projectSubMenuSet.AddMenuItem(newItemCommand);

        //სერვერების ჩამონათვალი
        if (project?.ServerInfos != null)
        {
            foreach (KeyValuePair<string, ServerInfoModel> kvp in
                     project.ServerInfos.OrderBy(o => o.Value.GetItemKey()))
                //, kvp.Value.GetItemKey()
            {
                projectSubMenuSet.AddMenuItem(new ServerInfoSubMenuCliMenuCommand(_appName, _logger, _httpClientFactory,
                    kvp.Value.GetItemKey(), _parametersManager, _projectName, kvp.Key, _serviceProvider));
            }
        }

        //მთავარ მენიუში გასვლა
        projectSubMenuSet.AddEscapeCommand();

        return projectSubMenuSet;
    }
}
