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

    //შედეგის ფერადი ნაწილები: სტატუსი და შეცდომებისა და გაფრთხილებების რაოდენობები (მხოლოდ ნულზე მეტი)
    public static List<StatusColorPart> BuildParts(ProjectBuildCheckResult result)
    {
        List<StatusColorPart> parts = [new StatusColorPart(GetName(result.Status), GetColor(result.Status))];
        AddCountParts(parts, result.ErrorCount, result.WarningCount);
        return parts;
    }

    //ტექსტური სტატუსი ზუსტად ემთხვევა ფერადი ნაწილების ტექსტს (მენიუ ნაწილებს ", "-ით აერთებს)
    public static string GetText(ProjectBuildCheckResult? result)
    {
        return result is null ? NotCheckedName : string.Join(", ", BuildParts(result).Select(p => p.Text));
    }

    //რაოდენობის ნაწილები ემატება მხოლოდ მაშინ, თუ რაოდენობა ნულზე მეტია: შეცდომები წითლად, გაფრთხილებები ყვითლად
    public static void AddCountParts(List<StatusColorPart> parts, int errorCount, int warningCount)
    {
        if (errorCount > 0)
        {
            parts.Add(CreateCountPart("errors", errorCount, ConsoleColor.Red));
        }

        if (warningCount > 0)
        {
            parts.Add(CreateCountPart("warnings", warningCount, ConsoleColor.Yellow));
        }
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

    private static StatusColorPart CreateCountPart(string label, int count, ConsoleColor color)
    {
        return new StatusColorPart($"{label}: {count.ToString(CultureInfo.InvariantCulture)}", color);
    }
}
