using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SupportToolsData.Models;

//„ამ კომპიუტერზეა“ (D3): პროექტის ProjectFolderName (ამ კომპიუტერის გზა, pull-ის შემდეგ უკვე გარდაქმნილი) არსებობს.
//სინქრონიზაციის შემდეგ ყველა პროექტი ყველა კომპიუტერზე ჩანს, ფიზიკურად კი მხოლოდ ზოგიერთზეა.
//მენიუ ამას ყოველ ხატვაზე ამოწმებს, ამიტომ შემოწმება მხოლოდ Directory.Exists-ია, ქეშის გარეშე
public static class ProjectPresence
{
    public static bool IsLocal(ProjectModel project)
    {
        return !string.IsNullOrWhiteSpace(project.ProjectFolderName) && Directory.Exists(project.ProjectFolderName);
    }

    //ჯგუფური ბრძანებების ფილტრი: includeRemote=false ტოვებს მხოლოდ ამ კომპიუტერზე არსებულ პროექტებს
    public static IEnumerable<KeyValuePair<string, ProjectModel>> Filter(
        IEnumerable<KeyValuePair<string, ProjectModel>> projects, bool includeRemote)
    {
        return includeRemote ? projects : projects.Where(x => IsLocal(x.Value));
    }
}
