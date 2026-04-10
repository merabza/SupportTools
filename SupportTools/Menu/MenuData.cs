using System.Collections.Generic;
using LibSupportToolsServerWork.Menu.SupportToolsServerEdit;
using SupportTools.Menu.SupportToolsParametersEdit;

namespace SupportTools.Menu;

public static class MenuData
{
    public static List<string> MenuCommandNames { get; } =
    [
        //ძირითადი პარამეტრების რედაქტირება
        SupportToolsParametersEditor.MenuCommandName,
        //სერვერის პარამეტრების რედაქტირება
        SupportToolsServerEditorCliMenuCommand.MenuCommandName
        ////პარამეტრების ფაილის შენახვა ლოკალური სერვისისთვის
        //SaveReplicatorParametersForLocalReplicatorServiceCommand.MenuCommandName,
        ////სამუშაოების დროის დაგეგმვების სია
        //JobScheduleCruder.MenuCommandName
    ];
}
