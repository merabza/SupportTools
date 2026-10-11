using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu;

//ჯგუფური ბრძანებები ნაგულისხმევად მხოლოდ ამ კომპიუტერზე არსებულ ([local]) პროექტებზე მუშაობს (D3, მომხმარებლის არჩევანი):
//- ბრძანებები, რომლებიც git-ებს სინქრონიზაციით კლონავს (Sync V2, Update Outdated Packages, Distribute all packages),
//  ეკითხება, ჩაირთოს თუ არა remote პროექტებიც (ნაგულისხმევად - არა);
//- დანარჩენები remote პროექტებს ყოველთვის ტოვებს: ფაილების გარეშე ისინი მხოლოდ „არ არსებობს“ გაფრთხილებებს დაწერდა
public static class RemoteOnlyProjects
{
    //კონსოლიდან პასუხი. ბრძანებები მას თავიანთ საჯარო კონსტრუქტორებში აწვდიან, ტესტები კი თავიანთ პასუხებს
    public static bool InputBoolFromConsole(string fieldName, bool defaultValue)
    {
        return Inputer.InputBool(fieldName, defaultValue, false);
    }

    //კითხვა ისმება მხოლოდ მაშინ, როცა remote პროექტი მართლა არის
    public static bool AskInclude(IEnumerable<KeyValuePair<string, ProjectModel>> projects,
        Func<string, bool, bool> inputBool)
    {
        int remoteCount = projects.Count(x => !ProjectPresence.IsLocal(x.Value));
        return remoteCount > 0 &&
               inputBool(
                   $"Include {remoteCount.ToString(CultureInfo.InvariantCulture)} remote-only projects (their gits will be cloned to this computer)?",
                   false);
    }

    public static List<KeyValuePair<string, ProjectModel>> Skip(
        IEnumerable<KeyValuePair<string, ProjectModel>> projects, ILogger? logger)
    {
        List<KeyValuePair<string, ProjectModel>> allProjects = [.. projects];
        List<KeyValuePair<string, ProjectModel>> localProjects = [.. ProjectPresence.Filter(allProjects, false)];

        int skippedCount = allProjects.Count - localProjects.Count;
        if (skippedCount > 0)
        {
            StShared.ConsoleWriteInformationLine(logger, true,
                $"{skippedCount.ToString(CultureInfo.InvariantCulture)} remote-only projects are skipped");
        }

        return localProjects;
    }
}
