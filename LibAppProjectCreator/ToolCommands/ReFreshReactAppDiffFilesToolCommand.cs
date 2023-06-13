//using System;
//using System.Diagnostics;
//using System.IO;
//using CliParameters;
//using Microsoft.Extensions.Logging;
//using SupportToolsData.Models;
//using SystemToolsShared;

//namespace SupportTools.ToolCommands;

//public sealed class ReFreshReactAppDiffFilesToolCommand : ToolCommand
//{
//    private readonly string _reactAppName;
//    private readonly string? _reactTemplateName;

//    public ReFreshReactAppDiffFilesToolCommand(ILogger logger, bool useConsole, string reactAppName,
//        string? reactTemplateName, IParameters par, IParametersManager? parametersManager, bool askRunAction = true) :
//        base(logger, useConsole, "ReFresh React App Diff Files", "ReFresh React App Diff Files", par, parametersManager,
//            askRunAction)
//    {
//        _reactAppName = reactAppName;
//        _reactTemplateName = reactTemplateName;
//    }

//    protected override bool RunAction()
//    {
//        var supportToolsParameters = (SupportToolsParameters?)ParametersManager?.Parameters;

//        if (ParametersManager is null)
//        {
//            Logger.LogError("ParametersManager is null");
//            return false;
//        }

//        if (supportToolsParameters is null)
//        {
//            Logger.LogError("SupportToolsParameters is null");
//            return false;
//        }

//        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
//        {
//            Logger.LogError("AppProjectCreatorAllParameters is empty");
//            return false;
//        }

//        string reactAppModelsFolderFullName = Path.Combine(supportToolsParameters.WorkFolder, "ReactAppModels");

//        var checkedPath = FileStat.CreateFolderIfNotExists(reactAppModelsFolderFullName, true);
//        if (checkedPath is null)
//        {
//            StShared.WriteErrorLine($"does not exists and cannot create work folder {reactAppModelsFolderFullName}", true, Logger);
//            return false;
//        }

//        var appName = _reactAppName.ToLower();

//        string appFolderFullName = Path.Combine(reactAppModelsFolderFullName, appName);

//        string reactAppModelsForDiffFolderFullName = Path.Combine(supportToolsParameters.WorkFolder, "ReactAppModelsForDiff");

//        var checkedDiffPath = FileStat.CreateFolderIfNotExists(reactAppModelsForDiffFolderFullName, true);
//        if (checkedDiffPath is null)
//        {
//            StShared.WriteErrorLine($"does not exists and cannot create work folder {reactAppModelsForDiffFolderFullName}", true, Logger);
//            return false;
//        }

//        string appFolderForDiffFullName = Path.Combine(reactAppModelsForDiffFolderFullName, appName);

//        string[] excludes = { ".git", "node_modules" };

//        if (Directory.Exists(appFolderForDiffFullName))
//            FileStat.ClearFolder(appFolderForDiffFullName, excludes, true, Logger);

//        FileStat.CopyFilesAndFolders(appFolderFullName, appFolderForDiffFullName, excludes, true, Logger);

//        //შემოწმდეს არის თუ არა დაინიცირებული appFolderForDiffFullName ფოლდერში გიტი
//        //თუ დაინიცირებულია ვჩერდებით
//        //თუ არა უბრალოდ ვაინიცირებთ და ვაკომიტებთ

//        if (StShared.RunProcessWithOutput(true, Logger, "git", $"-C \"{appFolderForDiffFullName}\" rev-parse --is-inside-work-tree") == "true"+ Environment.NewLine)
//            return true;

//        if (!StShared.RunProcess(true, Logger, "git", $"-C \"{appFolderForDiffFullName}\" init"))
//            return false;

//        if (!StShared.RunProcess(true, Logger, "git", $"-C \"{appFolderForDiffFullName}\" add ."))
//            return false;

//        if (!StShared.RunProcess(true, Logger, "git", $"-C \"{appFolderForDiffFullName}\" commit -m \"Initial\""))
//            return false;

//        return true;

//    }

//    private bool RunCmdProcess(string command, string? projectPath = null)
//    {
//        var psiNpmRunDist = new ProcessStartInfo
//        {
//            FileName = "cmd",
//            RedirectStandardInput = true,
//            WorkingDirectory = projectPath ?? Directory.GetCurrentDirectory()
//        };
//        var pNpmRunDist = Process.Start(psiNpmRunDist);
//        if (pNpmRunDist == null)
//            return false;
//        pNpmRunDist.StandardInput.WriteLine($"{command} & exit");
//        pNpmRunDist.WaitForExit();

//        return true;
//    }

//}

