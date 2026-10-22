using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AppCliTools.CliMenu;
using SupportToolsData.Models;

namespace SupportTools.Menu;

//„ამ კომპიუტერზეა“ (D3, ProjectPresence) მენიუში: პროექტის სტატუსის პირველი ნაწილი local ან remote,
//ჯგუფის სტატუსისა - local: N და remote: N (ნულოვანი რაოდენობა არ ჩანს). remote ნაცრისფრად იხატება
public static class ProjectPresenceView
{
    public const string LocalName = "local";
    public const string RemoteName = "remote";

    public static StatusColorPart CreatePart(bool isLocal)
    {
        return isLocal
            ? new StatusColorPart(LocalName, Console.ForegroundColor)
            : new StatusColorPart(RemoteName, ConsoleColor.DarkGray);
    }

    public static List<StatusColorPart> CreateCountParts(IEnumerable<ProjectModel> projects)
    {
        List<bool> presences = [.. projects.Select(ProjectPresence.IsLocal)];
        int localCount = presences.Count(x => x);
        int remoteCount = presences.Count - localCount;

        List<StatusColorPart> parts = [];
        if (localCount > 0)
        {
            parts.Add(new StatusColorPart($"{LocalName}: {localCount.ToString(CultureInfo.InvariantCulture)}",
                Console.ForegroundColor));
        }

        if (remoteCount > 0)
        {
            parts.Add(new StatusColorPart($"{RemoteName}: {remoteCount.ToString(CultureInfo.InvariantCulture)}",
                ConsoleColor.DarkGray));
        }

        return parts;
    }

    //სტატუსის ტექსტი ზუსტად ემთხვევა ფერადი ნაწილების ტექსტს (მენიუ ნაწილებს ", "-ით აერთებს)
    public static string GetText(IEnumerable<StatusColorPart> parts)
    {
        return string.Join(", ", parts.Select(p => p.Text));
    }

    //HideRemoteOnlyProjects-ისას სიებში მხოლოდ ამ კომპიუტერზე არსებული პროექტები რჩება
    public static IEnumerable<KeyValuePair<string, ProjectModel>> GetVisibleProjects(SupportToolsParameters parameters)
    {
        return ProjectPresence.Filter(parameters.Projects, !parameters.HideRemoteOnlyProjects);
    }
}
