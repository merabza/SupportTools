using System.Collections.Generic;
using AppCliTools.CliTools.CliMenuCommands;
using SupportTools.Menu.ClearAllGroupsAllSolutionsAllProjects;
using SupportTools.Menu.CreateNewProject;
using SupportTools.Menu.CreateProject;
using SupportTools.Menu.ImportProject;
using SupportTools.Menu.ProjectGroupsList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CreateNewServerInfo;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.DeleteProject;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.EditItemAllFieldsInSequence;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ExportProject;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectParametersList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectToolsList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;
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
        nameof(CreateNewProjectFactoryStrategy),
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
        //ჯგუფში შემავალი ყველა პროჯეცტის გიტების სინქრონიზაცია
        SyncOneGroupAllProjectsGitsCliMenuCommandV2.MenuCommandName,
        //პროექტების ჩამონავთვალი
        nameof(ProjectSubMenuCliMenuCommand)
    ];

    public static List<string> ProjectSubMenuCommandNames { get; } =
    [
        //პროექტის წაშლა
        DeleteProjectCliMenuCommand.MenuCommandName,
        //პროექტის ექსპორტი
        ExportProjectCliMenuCommand.MenuCommandName,
        //პროექტის Visual Studio-ში გახსნა
        OpenByVisualStudioCliMenuCommand.MenuCommandName,
        //პროექტის გიტების სინქრონიზაცია
        SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2.MenuCommandName,
        //პროექტის გიტების ჩამონათვალი და მართვა
        nameof(GitSubMenuCliMenuCommandFactoryStrategy),
        //პროექტის Scaffold Seeder-ის გიტების ჩამონათვალი და მართვა
        nameof(GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy),
        //დასაშვები ინსტრუმენტების არჩევის საშუალება
        nameof(SelectProjectAllowToolsCliMenuCommand),
        //დასაშვები ინსტრუმენტები
        nameof(ProjectToolsListFactoryStrategy),
        //ახალი პროექტის შექმნა 
        nameof(CreateNewServerInfoFactoryStrategy),
        //პროექტის პარამეტრების რედაქტირება თანმიმდევრობით
        nameof(EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy),
        //პროექტის პარამეტრების სია თავისი რედაქტორებით
        nameof(ProjectParametersListFactoryStrategy)
    ];
}
