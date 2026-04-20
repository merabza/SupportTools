using System.Collections.Generic;
using AppCliTools.CliTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportTools.Menu.ClearAllGroupsAllSolutionsAllProjects;
using SupportTools.Menu.CreateProject;
using SupportTools.Menu.ImportProject;
using SupportTools.Menu.ProjectGroupsList;
using SupportTools.Menu.ProjectGroupsList.ProjectList;
using SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportTools.Menu.SupportToolsServerEdit;
using SupportTools.Menu.SyncAllProjectsAllGits;
using SupportTools.Menu.UpdateOutdatedPackages;

namespace SupportTools.Menu;

public static class MenuData
{
    public static List<string> MenuCommandNames { get; } =
    [
        //ძირითადი პარამეტრების რედაქტირება
        SupportToolsParametersEditor.MenuCommandName,
        //სერვერის პარამეტრების რედაქტირება
        SupportToolsServerEditorCliMenuCommand.MenuCommandName,
        //ახალი პროექტების შემქმნელი სუბმენიუ
        ProjectCreatorSubMenuCliMenuCommand.MenuCommandName,
        //ახალი პროექტის შექმნა 
        $"New {ProjectCruder.MenuCommandName}",
        //პროექტის დაიმპორტება
        ImportProjectCliMenuCommand.MenuCommandName,
        //ყველა პროექტის git-ის სინქრონიზაცია V2
        SyncAllProjectsAllGitsCliMenuCommandV2.MenuCommandName,
        //ყველა პროექტის პაკეტების განახლება
        UpdateOutdatedPackagesCliMenuCommand.MenuCommandName,
        //ყველა ჯგუფების, ყველა სოლუშენის, ყველა პროექტის გასუფთავება
        ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand.MenuCommandName,
        //პროექტების ჯგუფების ჩამონათვალი
        nameof(ProjectGroupSubMenuCliMenuCommand),
        //ბოლოს გამოყენებული ბრძანებების ჩამონათვალი
        nameof(RecentCommandCliMenuCommand)
    ];

    public static List<string> ProjectGroupSubMenuCommandNames { get; } =
    [
        SyncOneGroupAllProjectsGitsCliMenuCommandV2.MenuCommandName, nameof(ProjectSubMenuCliMenuCommand)
    ];
}
