//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using LibDotnetWork;
//using OneOf;
//using SupportTools.Errors;
//using SupportTools.Models;
//using SystemToolsShared;
//using SystemToolsShared.Errors;

//// ReSharper disable ConvertToPrimaryConstructor

//namespace SupportTools.DotnetTools;

//public sealed class DotnetToolsManager
//{
//    private static DotnetToolsManager? _instance;
//    private static readonly Lock SyncRoot = new();
//    private readonly Dictionary<ENecessaryTools, string> _necessaryToolsNames;

//    // ReSharper disable once MemberCanBePrivate.Global
//    public DotnetToolsManager(Dictionary<ENecessaryTools, string> necessaryToolsNames, List<DotnetTool> dotnetTools)
//    {
//        _necessaryToolsNames = necessaryToolsNames;
//        DotnetTools = dotnetTools;
//    }

//    public List<DotnetTool> DotnetTools { get; private set; }

//    public static DotnetToolsManager? Instance
//    {
//        get
//        {
//            //ეს ატრიბუტები სესიაზე არ არის დამოკიდებული და იქმნება პროგრამის გაშვებისთანავე, 
//            //შემდგომში მასში ცვლილებები არ შედის,
//            //მაგრამ შეიძლება პროგრამამ თავისი მუშაობის განმავლობაში რამდენჯერმე გამოიყენოს აქ არსებული ინფორმაცია
//            if (_instance != null)
//                return _instance;
//            lock (SyncRoot) //thread safe singleton
//            {
//                var createResult = Create();
//                if (createResult.IsT1)
//                {
//                    Err.PrintErrorsOnConsole(createResult.AsT1);
//                    return null;
//                }

//                _instance = createResult.AsT0;
//            }

//            return _instance;
//        }
//    }

//    private static OneOf<DotnetToolsManager, IEnumerable<Err>> Create()
//    {
//        StShared.ConsoleWriteInformationLine(null, true, "Wait...");
//        var necessaryToolsNames = new Dictionary<ENecessaryTools, string>
//        {
//            //ეს არის Entity Framework-ის ინსტრუმენტი, რომელიც გამოიყენება ბაზის მოდელის გაკეთებისას
//            { ENecessaryTools.DotnetEf, "dotnet-ef" }, //ef
//            //ეს არის ინსტრუმენტი, რომელიც ამოწმებს სოლუშენში არსებული პროექტებში პაკეტების ვერსიებს და თუ განახლებაა საჭიროა გვეხმარება განახლებაში
//            //რადგან შესაძლებელია dotnet list package --outdated ბრძანების გამოყენება, შეიძლება ის რასაც ეს ინსტრუმენტი აკეთებს, პირდაპირ გააკეთოს ჩვენმა პროგრამამ
//            //საჭიროა გაირჩეს json-ი, რომელსაც აბრუნებს ეს ბრძანება და კიდევ რა ჯობია სოლუშენზე გავაკეთოთ, თუ სათითაოდ პროექტებზე.
//            {
//                ENecessaryTools.DotnetOutdatedTool, "dotnet-outdated-tool" //outdated
//            }, //ამის გამოყენება ხდება შემდეგნაირად dotnet outdated და განახლების გაშვებისთვის dotnet outdated -u
//            //ეს არის რეშარპერის ინსტრუმენტი, რომელიც აანალიზებს კოდს და ასწორებს ფორმატს და ზოგიერთ სხვა რამეს
//            { ENecessaryTools.JetbrainsReSharperGlobalTools, "jetbrains.resharper.globaltools" } //jb
//        };
//        var createListOfDotnetToolsResult = CreateListOfDotnetTools(necessaryToolsNames);
//        if (createListOfDotnetToolsResult.IsT1)
//            return Err.RecreateErrors(createListOfDotnetToolsResult.AsT1,
//                DotnetToolsManagerErrors.CreateListOfDotnetToolsError);
//        var dotnetTools = createListOfDotnetToolsResult.AsT0;
//        return new DotnetToolsManager(necessaryToolsNames, dotnetTools);
//    }

//    private static OneOf<List<DotnetTool>, IEnumerable<Err>> CreateListOfDotnetTools(
//        Dictionary<ENecessaryTools, string> necessaryToolsNames)
//    {
//        var createListOfDotnetToolsInstalledResult = CreateListOfDotnetToolsInstalled();
//        if (createListOfDotnetToolsInstalledResult.IsT1)
//            return Err.RecreateErrors(createListOfDotnetToolsInstalledResult.AsT1,
//                DotnetToolsManagerErrors.CreateListOfDotnetToolsInstalledError);
//        var listOfTools = createListOfDotnetToolsInstalledResult.AsT0;

//        foreach (var pair in necessaryToolsNames)
//        {
//            var getAvailableVersionOfToolResult = GetAvailableVersionOfTool(pair.Value);
//            if (getAvailableVersionOfToolResult.IsT1)
//                return Err.RecreateErrors(getAvailableVersionOfToolResult.AsT1,
//                    DotnetToolsManagerErrors.GetAvailableVersionOfToolError);
//            var availableVersion = getAvailableVersionOfToolResult.AsT0;
//            var nesTool = listOfTools.FirstOrDefault(tool => tool.PackageId == pair.Value);
//            if (nesTool is null)
//                listOfTools.Add(new DotnetTool(pair.Value, "N/A", availableVersion, string.Empty));
//            else
//                nesTool.AvailableVersion = availableVersion;
//        }

//        return listOfTools;
//    }

//    private static OneOf<string, IEnumerable<Err>> GetAvailableVersionOfTool(string toolName)
//    {
//        var dotnetProcessor = new DotnetProcessor(null, false);
//        var processResult = dotnetProcessor.SearchTool(toolName);
//        if (processResult.IsT1)
//            return (Err[])processResult.AsT1;
//        var outputResult = processResult.AsT0.Item1;
//        var outputLines = outputResult.Split(Environment.NewLine);
//        if (outputLines.Length < 3) return "N/A";
//        var lineParts = outputLines[2].Split(" ", StringSplitOptions.RemoveEmptyEntries);
//        return lineParts.Length < 2 ? "N/A" : lineParts[1];
//    }

//    private static OneOf<List<DotnetTool>, IEnumerable<Err>> CreateListOfDotnetToolsInstalled()
//    {
//        var dotnetProcessor = new DotnetProcessor(null, false);
//        var getToolsRawListResult = dotnetProcessor.GetToolsRawList();
//        if (getToolsRawListResult.IsT1)
//            return (Err[])getToolsRawListResult.AsT1;

//        var listOfTools = getToolsRawListResult.AsT0.Skip(2)
//            .Select(line => line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
//            .Where(lineParts => lineParts.Length == 3)
//            .Select(lineParts => new DotnetTool(lineParts[0], lineParts[1], null, lineParts[2])).ToList();

//        return listOfTools;
//    }

//    public void UpdateAllToolsToLatestVersion()
//    {
//        StShared.ConsoleWriteInformationLine(null, true, "Checking for tools Updates...");
//        var createListOfDotnetToolsResult = CreateListOfDotnetTools(_necessaryToolsNames);
//        if (createListOfDotnetToolsResult.IsT1)
//        {
//            Err.PrintErrorsOnConsole(createListOfDotnetToolsResult.AsT1);
//            return;
//        }

//        DotnetTools = createListOfDotnetToolsResult.AsT0;

//        var atLeastOneUpdatedOrInstalled = false;
//        foreach (var tool in DotnetTools)
//        {
//            if (tool.AvailableVersion is null or "N/A" || tool.Version == tool.AvailableVersion) continue;
//            var toolInstalled = tool.Version != "N/A";
//            var command = toolInstalled ? "update" : "install";
//            StShared.ConsoleWriteInformationLine(null, true, "{0}ing {1}...", command, tool.PackageId);

//            var dotnetProcessor = new DotnetProcessor(null, false);
//            var result = toolInstalled
//                ? dotnetProcessor.UpdateTool(tool.PackageId)
//                : dotnetProcessor.InstallTool(tool.PackageId);

//            if (result.IsSome)
//                return;
//            atLeastOneUpdatedOrInstalled = true;
//        }

//        if (atLeastOneUpdatedOrInstalled)
//        {
//            StShared.ConsoleWriteInformationLine(null, true, "Updating tools List...");
//            createListOfDotnetToolsResult = CreateListOfDotnetTools(_necessaryToolsNames);
//            if (createListOfDotnetToolsResult.IsT1)
//            {
//                Err.PrintErrorsOnConsole(createListOfDotnetToolsResult.AsT1);
//                return;
//            }

//            DotnetTools = createListOfDotnetToolsResult.AsT0;
//            StShared.ConsoleWriteInformationLine(null, true, "Updating process Finished.");
//        }
//        else
//        {
//            StShared.ConsoleWriteInformationLine(null, true, "All tools already are up to date.");
//        }

//        StShared.Pause();
//    }
//}