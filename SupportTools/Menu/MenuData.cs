using System.Collections.Generic;
using LibGitWork.Menu.SyncAllProjectsAllGits;
using LibSupportToolsServerWork.Menu.SupportToolsServerEdit;
using SupportTools.Cruders;
using SupportTools.Menu.CreateProject;
using SupportTools.Menu.ImportProject;
using SupportTools.Menu.SupportToolsParametersEdit;

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
        SyncAllProjectsAllGitsCliMenuCommandV2.MenuCommandName

    ];
}
