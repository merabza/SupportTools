using System.Collections.Generic;
using AppCliTools.CliTools.Menu.RecentCommandsList;
using SupportTools.Menu.BaGetter;
using SupportTools.Menu.CheckAllProjectsBuild;
using SupportTools.Menu.ClearAllGroupsAllSolutionsAllProjects;
using SupportTools.Menu.CreateNewProject;
using SupportTools.Menu.CreateProject;
using SupportTools.Menu.ImportProject;
using SupportTools.Menu.ProjectGroupsList;
using SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsBuild;
using SupportTools.Menu.ProjectGroupsList.OpenAllProjectsByVisualStudio;
using SupportTools.Menu.ProjectGroupsList.ProjectsList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectBuild;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CreateNewServerInfo;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.DeleteProject;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.EditItemAllFieldsInSequence;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ExportProject;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectParametersList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectToolsList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ServerInfosList;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;
using SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;
using SupportTools.Menu.ReversePackageDistribution;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportTools.Menu.SupportToolsServerEdit;
using SupportTools.Menu.SyncAllProjectsAllGits;
using SupportTools.Menu.UpdateOutdatedPackages;

namespace SupportTools.Menu;

public static class MenuData
{
    public static List<string> MainMenuCommandFactoryStrategyNames { get; } =
    [
        //ძირითადი პარამეტრების რედაქტირება
        nameof(SupportToolsParametersEditorListCliMenuCommandFactoryStrategy),
        //სერვერის პარამეტრების რედაქტირება
        nameof(SupportToolsServerEditorListCliMenuCommandFactoryStrategy),
        //ახალი პროექტების შემქმნელი სუბმენიუ
        nameof(ProjectCreatorSubMenuCliMenuCommandFactoryStrategy),
        //ახალი პროექტის შექმნა
        nameof(CreateNewProjectFactoryStrategy),
        //პროექტის დაიმპორტება
        nameof(ImportProjectCliMenuCommandFactoryStrategy),
        //ყველა პროექტის git-ის სინქრონიზაცია V2
        nameof(SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy),
        //ყველა პროექტის პაკეტების განახლება
        nameof(UpdateOutdatedPackagesCliMenuCommandFactoryStrategy),
        //ყველა პროექტის მთავარ რეპოზიტორიაში საკუთარი პაკეტების ჩანაცვლება პროექტის რეფერენსებით
        nameof(ReversePackageDistributionCliMenuCommandFactoryStrategy),
        //ყველა ჯგუფების, ყველა სოლუშენის, ყველა პროექტის გასუფთავება
        nameof(ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy),
        //ყველა პროექტის build-ის შემოწმება
        nameof(CheckAllProjectsBuildCliMenuCommandFactoryStrategy),
        //BaGetter პაკეტების სერვერზე პაკეტების ჩამონათვალი და მართვა
        nameof(BaGetterSubMenuCliMenuCommandFactoryStrategy),
        //პროექტების ჯგუფების ჩამონათვალი
        nameof(ProjectGroupsListFactoryStrategy),
        //ბოლოს გამოყენებული ბრძანებების ჩამონათვალი
        nameof(RecentCommandsListFactoryStrategy)
    ];

    public static List<string> ProjectGroupSubMenuCommandFactoryStrategyNames { get; } =
    [
        //ჯგუფში შემავალი ყველა პროჯეცტის გიტების სინქრონიზაცია
        nameof(SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy),
        //ჯგუფში შემავალი ყველა პროექტის build-ის შემოწმება
        nameof(CheckGroupProjectsBuildCliMenuCommandFactoryStrategy),
        //ჯგუფში შემავალი ყველა პროექტის Visual Studio-ში გახსნა
        nameof(OpenAllProjectsByVisualStudioCliMenuCommandFactoryStrategy),
        //პროექტების ჩამონავთვალი
        nameof(ProjectsListFactoryStrategy)
    ];

    public static List<string> ProjectSubMenuCommandFactoryStrategyNames { get; } =
    [
        //პროექტის წაშლა
        nameof(DeleteProjectCliMenuCommandFactoryStrategy),
        //პროექტის ექსპორტი
        nameof(ExportProjectCliMenuCommandFactoryStrategy),
        //პროექტის Visual Studio-ში გახსნა
        nameof(OpenByVisualStudioCliMenuCommandFactoryStrategy),
        //პროექტის build-ის შემოწმება
        nameof(CheckOneProjectBuildCliMenuCommandFactoryStrategy),
        //პაკეტის სოლუშენის შემოწმება (მხოლოდ პაკეტის ტიპის პროექტებისთვის)
        nameof(CheckPackageSolutionCliMenuCommandFactoryStrategy),
        //პაკეტის დაბილდვა და ატვირთვა package manager-ზე (მხოლოდ პაკეტის ტიპის პროექტებისთვის)
        nameof(BuildPackageCliMenuCommandFactoryStrategy),
        //პაკეტის გავრცელება მომხმარებელ პროექტებში (მხოლოდ პაკეტის ტიპის პროექტებისთვის)
        nameof(PackageDistributionCliMenuCommandFactoryStrategy),
        //პროექტის გიტების სინქრონიზაცია
        nameof(SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy),
        //პროექტის გიტების ჩამონათვალი და მართვა
        nameof(GitSubMenuCliMenuCommandFactoryStrategy),
        //პროექტის Scaffold Seeder-ის გიტების ჩამონათვალი და მართვა
        nameof(GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy),
        //დასაშვები ინსტრუმენტების არჩევის საშუალება
        nameof(SelectProjectAllowToolsFactoryStrategy),
        //დასაშვები ინსტრუმენტები
        nameof(ProjectToolsListFactoryStrategy),
        //ცხრილების და ველების წყვილების ფაილის რედაქტირება
        nameof(PairedDbObjectsSubMenuCliMenuCommandFactoryStrategy),
        //ახალი სერვერის ინფორმაციის შექმნა
        nameof(CreateNewServerInfoFactoryStrategy),
        //სერვერების ინფორმაციის ჩამონათვალი და მართვა
        nameof(ServerInfosListFactoryStrategy),
        //პროექტის პარამეტრების რედაქტირება თანმიმდევრობით
        nameof(EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy),
        //პროექტის პარამეტრების სია თავისი რედაქტორებით
        nameof(ProjectParametersListFactoryStrategy)
    ];
}
