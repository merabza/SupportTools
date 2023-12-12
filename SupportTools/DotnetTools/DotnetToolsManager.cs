using System;
using System.Collections.Generic;
using System.Linq;
using SupportTools.Models;
using SystemToolsShared;

namespace SupportTools.DotnetTools;

public sealed class DotnetToolsManager
{
    private static DotnetToolsManager? _instance;
    private static readonly object SyncRoot = new();
    private readonly Dictionary<ENecessaryTools, string> _necessaryToolsNames;

    // ReSharper disable once MemberCanBePrivate.Global
    private DotnetToolsManager()
    {
        StShared.ConsoleWriteInformationLine(null, true,"Wait...");
        _necessaryToolsNames = new Dictionary<ENecessaryTools, string>
        {
            //ეს არის Entity Framework-ის ინსტრუმენტი, რომელიც გამოიყენება ბაზის მოდელის გაკეთებისას
            { ENecessaryTools.DotnetEf, "dotnet-ef" },
            //ეს არის ინსტრუმენტი, რომელიც ამოწმებს სოლუშენში არსებული პროექტებში პაკეტების ვერსიებს და თუ განახლებაა საჭიროა გვეხმარება განახლებაში
            //რადგან შესაძლებელია dotnet list package --outdated ბრძანების გამოყენება, შეიძლება ის რასაც ეს ინსტრუმენტი აკეთებს, პირდაპირ გააკეთოს ჩვენმა პროგრამამ
            //საჭიროა გაირჩეს json-ი, რომელსაც აბრუნებს ეს ბრძანება და კიდევ რა ჯობია სოლუშენზე გავაკეთოთ, თუ სათითაოდ პროექტებზე.
            {
                ENecessaryTools.DotnetOutdatedTool, "dotnet-outdated-tool"
            }, //ამის გამოყენება ხდება შემდეგნაირად dotnet outdated და განახლების გაშვებისთვის dotnet outdated -u
            //ეს არის რეშარპერის ინსტრუმენტი, რომელიც აანალიზებს კოდს და ასწორებს ფორმატს და ზოგიერთ სხვა რამეს
            { ENecessaryTools.JetbrainsReSharperGlobalTools, "jetbrains.resharper.globaltools" }
        };
        DotnetTools = CreateListOfDotnetTools();
    }


    public List<DotnetTool> DotnetTools { get; private set; }

    public static DotnetToolsManager Instance
    {
        get
        {
            //ეს ატრიბუტები სესიაზე არ არის დამოკიდებული და იქმნება პროგრამის გაშვებისთანავე, 
            //შემდგომში მასში ცვლილებები არ შედის,
            //მაგრამ შეიძლება პროგრამამ თავისი მუშაობის განმავლობაში რამდენჯერმე გამოიყენოს აქ არსებული ინფორმაცია
            if (_instance != null)
                return _instance;
            lock (SyncRoot) //thread safe singleton
            {
                _instance ??= new DotnetToolsManager();
            }

            return _instance;
        }
    }

    private List<DotnetTool> CreateListOfDotnetTools()
    {
        var listOfTools = CreateListOfDotnetToolsInstalled();

        foreach (var pair in _necessaryToolsNames)
        {
            var availableVersion = GetAvailableVersionOfTool(pair.Value);
            var nesTool = listOfTools.FirstOrDefault(tool => tool.PackageId == pair.Value);
            if (nesTool is null)
                listOfTools.Add(new DotnetTool(pair.Value, "N/A", availableVersion, ""));
            else
                nesTool.AvailableVersion = availableVersion;
        }

        return listOfTools;
    }

    private static string GetAvailableVersionOfTool(string toolName)
    {
        var outputResult = StShared.RunProcessWithOutput(false, null, "dotnet", $"tool search {toolName} --take 1");
        var outputLines = outputResult.Split(Environment.NewLine);
        if (outputLines.Length < 3) return "N/A";
        var lineParts = outputLines[2].Split(" ", StringSplitOptions.RemoveEmptyEntries);
        return lineParts.Length < 2 ? "N/A" : lineParts[1];
    }

    private static List<DotnetTool> CreateListOfDotnetToolsInstalled()
    {
        var outputResult = StShared.RunProcessWithOutput(false, null, "dotnet", "tool list --global");
        var outputLines = outputResult.Split(Environment.NewLine);

        var listOfTools = outputLines.Skip(2).Select(line => line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
            .Where(lineParts => lineParts.Length == 3)
            .Select(lineParts => new DotnetTool(lineParts[0], lineParts[1], null, lineParts[2])).ToList();

        return listOfTools;
    }

    public void UpdateAllToolsToLatestVersion()
    {
        StShared.ConsoleWriteInformationLine(null, true, "Checking for tools Updates...");
        DotnetTools = CreateListOfDotnetTools();

        var atLeastOneUpdatedOrInstalled = false;
        foreach (var tool in DotnetTools)
        {
            if (tool.AvailableVersion is null or "N/A" ||
                tool.Version == tool.AvailableVersion) continue;
            var command = tool.Version == "N/A" ? "install" : "update";
            StShared.ConsoleWriteInformationLine(null, true, "{0}ing {1}...", command, tool.PackageId);
            StShared.RunProcessWithOutput(false, null, "dotnet", $"tool {command} --global {tool.PackageId}");
            atLeastOneUpdatedOrInstalled = true;
        }

        if (atLeastOneUpdatedOrInstalled)
        {
            StShared.ConsoleWriteInformationLine(null, true, "Updating tools List...");
            DotnetTools = CreateListOfDotnetTools();
            StShared.ConsoleWriteInformationLine(null, true, "Updating process Finished.");
        }
        else
        {
            StShared.ConsoleWriteInformationLine(null, true, "All tools already are up to date.");
        }

        StShared.Pause();
    }
}