using System.Collections.Generic;

namespace SupportTools.Menu;

public static class MenuData
{
    public static List<string> MenuCommandNames { get; } =
    [
        //ძირითადი პარამეტრების რედაქტირება
        ReplicatorParametersEditor.MenuCommandName,
        //პარამეტრების ფაილის შენახვა ლოკალური სერვისისთვის
        SaveReplicatorParametersForLocalReplicatorServiceCommand.MenuCommandName,
        //სამუშაოების დროის დაგეგმვების სია
        JobScheduleCruder.MenuCommandName
    ];
}
