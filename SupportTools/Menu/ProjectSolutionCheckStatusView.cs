using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AppCliTools.CliMenu;
using SupportToolsData;

namespace SupportTools.Menu;

//"Check Solutions"-ის შედეგების ტექსტად და ფერად ჩვენების დამხმარე
public static class ProjectSolutionCheckStatusView
{
    public static string GetName(EProjectSolutionCheckStatus status)
    {
        return status switch
        {
            EProjectSolutionCheckStatus.NeedsAttention => "Needs attention",
            EProjectSolutionCheckStatus.NeedsDistributePackages => "Needs Distribute Packages",
            _ => "OK"
        };
    }

    public static ConsoleColor GetColor(EProjectSolutionCheckStatus status)
    {
        return status switch
        {
            //Needs attention - წითელი
            EProjectSolutionCheckStatus.NeedsAttention => ConsoleColor.Red,
            //Needs Distribute Packages - ლურჯი
            EProjectSolutionCheckStatus.NeedsDistributePackages => ConsoleColor.Blue,
            //OK - მწვანე
            _ => ConsoleColor.Green
        };
    }

    //პროექტის სტატუსის ნაწილი
    public static StatusColorPart CreatePart(ProjectSolutionCheckResult result)
    {
        return new StatusColorPart(GetName(result.Status), GetColor(result.Status));
    }

    //ჯგუფის სტატუსის ნაწილები: შემოწმებული პროექტების რაოდენობები სტატუსების მიხედვით, ყველაზე ცუდით დაწყებული.
    //ნულოვანი რაოდენობა არ ჩანს
    public static List<StatusColorPart> CreateCountParts(IEnumerable<ProjectSolutionCheckResult> results)
    {
        return
        [
            .. results.GroupBy(x => x.Status).OrderBy(g => g.Key).Select(g => new StatusColorPart(
                $"{GetName(g.Key)}: {g.Count().ToString(CultureInfo.InvariantCulture)}", GetColor(g.Key)))
        ];
    }

    //ერთი პროექტის შედეგი კონსოლში: სტატუსი თავისი ფერით და, თუ არის, მიზეზები
    public static void WriteResultLine(string projectName, ProjectSolutionCheckResult result)
    {
        ConsoleColor existingColor = Console.ForegroundColor;
        Console.Write($"{projectName}: ");
        Console.ForegroundColor = GetColor(result.Status);
        Console.Write(GetName(result.Status));
        Console.ForegroundColor = existingColor;
        Console.WriteLine(result.Details.Count == 0 ? string.Empty : $" ({string.Join("; ", result.Details)})");
    }
}
