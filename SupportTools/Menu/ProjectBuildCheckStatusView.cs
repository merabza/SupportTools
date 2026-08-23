using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AppCliTools.CliMenu;
using SupportToolsData;

namespace SupportTools.Menu;

//"Check ... build"-ის შედეგების ტექსტად და ფერად ჩვენების დამხმარე
public static class ProjectBuildCheckStatusView
{
    private const string NotCheckedName = "NotChecked";

    public static string GetName(EProjectBuildCheckStatus? status)
    {
        return status?.ToString() ?? NotCheckedName;
    }

    public static ConsoleColor GetColor(EProjectBuildCheckStatus? status)
    {
        return status switch
        {
            //Success - მწვანე
            EProjectBuildCheckStatus.Success => ConsoleColor.Green,
            //SuccessWithWarnings - ყვითელი
            EProjectBuildCheckStatus.SuccessWithWarnings => ConsoleColor.Yellow,
            //BuildFailed - წითელი
            EProjectBuildCheckStatus.BuildFailed => ConsoleColor.Red,
            //დანარჩენი სტატუსები - ლურჯი
            _ => ConsoleColor.Blue
        };
    }

    //შედეგის ფერადი ნაწილები: სტატუსი და, თუ build გაეშვა, შეცდომებისა და გაფრთხილებების რაოდენობები
    public static List<StatusColorPart> BuildParts(ProjectBuildCheckResult result)
    {
        List<StatusColorPart> parts = [new StatusColorPart(GetName(result.Status), GetColor(result.Status))];
        if (!result.IsBuildExecuted)
        {
            return parts;
        }

        parts.Add(CreateErrorCountPart(result.ErrorCount));
        parts.Add(CreateWarningCountPart(result.WarningCount));
        return parts;
    }

    //ტექსტური სტატუსი ზუსტად ემთხვევა ფერადი ნაწილების ტექსტს (მენიუ ნაწილებს ", "-ით აერთებს)
    public static string GetText(ProjectBuildCheckResult? result)
    {
        return result is null ? NotCheckedName : string.Join(", ", BuildParts(result).Select(p => p.Text));
    }

    public static StatusColorPart CreateErrorCountPart(int errorCount)
    {
        return CreateCountPart("errors", errorCount, ConsoleColor.Red);
    }

    public static StatusColorPart CreateWarningCountPart(int warningCount)
    {
        return CreateCountPart("warnings", warningCount, ConsoleColor.Yellow);
    }

    //ერთი პროექტის შედეგის კონსოლში გამოტანა build-ის დასრულების შემდეგ, ნაწილები თავიანთი ფერებით
    public static void WriteResultLine(string projectName, ProjectBuildCheckResult result)
    {
        ConsoleColor existingColor = Console.ForegroundColor;
        Console.Write($"{projectName}: ");
        List<StatusColorPart> parts = BuildParts(result);
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                Console.ForegroundColor = existingColor;
                Console.Write(", ");
            }

            Console.ForegroundColor = parts[i].Color;
            Console.Write(parts[i].Text);
        }

        Console.ForegroundColor = existingColor;
        Console.WriteLine();
    }

    //რაოდენობის ნაწილი: ნულზე მეტი - შესაბამისი ფერით, ნული - მწვანედ
    private static StatusColorPart CreateCountPart(string label, int count, ConsoleColor nonZeroColor)
    {
        return new StatusColorPart($"{label}: {count.ToString(CultureInfo.InvariantCulture)}",
            count > 0 ? nonZeroColor : ConsoleColor.Green);
    }
}
