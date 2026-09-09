using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibDotnetWork;
using LibGitData;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibGitWork.ToolActions;

public sealed class PackageUpdaterToolAction : ToolAction
{
    private const string CSharp = "CSharp";

    //dotnet outdated-ის შიდა race დროებითია, განმეორება თითქმის ყოველთვის გადის
    private const int MaxAttempts = 3;
    private readonly string _gitIgnorePathName;
    private readonly ILogger? _logger;
    private readonly string _projectFolderName;

    // ReSharper disable once ConvertToPrimaryConstructor
    private PackageUpdaterToolAction(ILogger? logger, GitSyncParameters gitSyncParameters) : base(logger, "Git Sync",
        null, null)
    {
        _logger = logger;
        _projectFolderName = Path.Combine(gitSyncParameters.GitsFolder, gitSyncParameters.GitData.GitProjectFolderName);
        _gitIgnorePathName = gitSyncParameters.GitData.GitIgnorePathName;
    }

    public static PackageUpdaterToolAction? Create(ILogger? logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol, string gitProjectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        var gitSyncParameters = GitSyncParameters.Create(loggerOrNull, supportToolsParameters, projectName, gitCol,
            gitProjectName, useConsole);

        if (gitSyncParameters is not null)
        {
            return new PackageUpdaterToolAction(loggerOrNull, gitSyncParameters);
        }

        StShared.WriteErrorLine("GitSyncParameters is not created", true);
        return null;
    }

    //აბრუნებს false-ს, თუ dotnet outdated ყველა ცდაზე ჩავარდა. პაუზა არ კეთდება, რომ დანარჩენი რეპოების დამუშავება
    //არ შეფერხდეს - ჩავარდნილი რეპოების შეჯამებას UpdateOutdatedPackagesToolAction ბოლოს ბეჭდავს
    public bool RunPackageUpdate()
    {
        if (_gitIgnorePathName != CSharp)
        {
            return true;
        }

        var dotnetProcessor = new DotnetProcessor(_logger, true);

        RestoreBeforeOutdated(dotnetProcessor);

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            Result<(string, int)> localResult =
                dotnetProcessor.UpdateOutdatedPackagesForProjectFolder(_projectFolderName, false);
            if (localResult.IsSuccess)
            {
                return true;
            }

            string message =
                $"dotnet outdated failed for {_projectFolderName} (attempt {attempt} of {MaxAttempts}){Environment.NewLine}{localResult.Error.Description}";
            if (attempt < MaxAttempts)
            {
                StShared.WriteWarningLine(message, true, _logger);
            }
            else
            {
                StShared.WriteErrorLine(message, true, _logger, false);
            }
        }

        return false;
    }

    //dotnet outdated პროექტებს პარალელურად აანალიზებს და თითოეულზე რეკურსიულ restore-ს უშვებს, რომლებიც საერთო
    //obj-ფაილებზე ერთმანეთს ეჯახება ("Cannot create a file when that file already exists"). წინასწარი restore ამ
    //გაშვებებს no-op-ად აქცევს. კეთდება მხოლოდ მაშინ, როცა ფოლდერში ზუსტად ერთი სოლუშენია; ჩავარდნა არ აჩერებს,
    //dotnet outdated მაინც გაეშვება
    private void RestoreBeforeOutdated(DotnetProcessor dotnetProcessor)
    {
        if (!Directory.Exists(_projectFolderName))
        {
            return;
        }

        List<string> solutionFiles = [.. Directory.EnumerateFiles(_projectFolderName).Where(IsSolutionFile)];
        if (solutionFiles.Count != 1)
        {
            return;
        }

        if (dotnetProcessor.RestoreForOutdated(solutionFiles[0]).IsFailure)
        {
            StShared.WriteWarningLine($"restore before dotnet outdated failed for {solutionFiles[0]}, continuing", true,
                _logger);
        }
    }

    private static bool IsSolutionFile(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }
}
