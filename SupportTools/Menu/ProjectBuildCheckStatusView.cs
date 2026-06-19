using System;
using SupportToolsData;

namespace SupportTools.Menu;

//"Check all projects build"-ის სტატუსების ტექსტად და ფერად ჩვენების დამხმარე
public static class ProjectBuildCheckStatusView
{
    public static string GetName(EProjectBuildCheckStatus? status)
    {
        return status?.ToString() ?? "NotChecked";
    }

    public static ConsoleColor GetColor(EProjectBuildCheckStatus? status)
    {
        return status switch
        {
            //Success - მწვანე
            EProjectBuildCheckStatus.Success => ConsoleColor.Green,
            //BuildFailed - წითელი
            EProjectBuildCheckStatus.BuildFailed => ConsoleColor.Red,
            //დანარჩენი სტატუსები - ლურჯი
            _ => ConsoleColor.Blue
        };
    }
}
