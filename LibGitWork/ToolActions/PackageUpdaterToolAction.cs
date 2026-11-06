using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGitWork.OutdatedPackages;
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
    internal const string CSharp = "CSharp";

    //dotnet outdated-ის შიდა race დროებითია, განმეორება თითქმის ყოველთვის გადის
    private const int MaxAttempts = 3;
    private readonly IPackageUpdaterCommands _commands;
    private readonly string _gitIgnorePathName;
    private readonly ILogger? _logger;
    private readonly string _projectFolderName;

    private PackageUpdaterToolAction(ILogger? logger, GitSyncParameters gitSyncParameters) : this(logger,
        Path.Combine(gitSyncParameters.GitsFolder, gitSyncParameters.GitData.GitProjectFolderName),
        gitSyncParameters.GitData.GitIgnorePatternName, new DotnetPackageUpdaterCommands(logger))
    {
    }

    //dotnet-ის ბრძანებები პარამეტრადაა გამოტანილი, რომ ტესტებმა restore და dotnet outdated არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal PackageUpdaterToolAction(ILogger? logger, string projectFolderName, string gitIgnorePathName,
        IPackageUpdaterCommands commands) : base(logger, "Git Sync", null, null)
    {
        _logger = logger;
        _projectFolderName = projectFolderName;
        _gitIgnorePathName = gitIgnorePathName;
        _commands = commands;
    }

    public static PackageUpdaterToolAction? Create(ILogger? logger, IParametersManager parametersManager,
        string projectName, string gitProjectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        var gitSyncParameters = GitSyncParameters.Create(loggerOrNull, supportToolsParameters, projectName,
            gitProjectName, useConsole);

        if (gitSyncParameters is not null)
        {
            return new PackageUpdaterToolAction(loggerOrNull, gitSyncParameters);
        }

        StShared.WriteErrorLine("GitSyncParameters is not created", true);
        return null;
    }

    //აბრუნებს OutdatedFailed-ს, თუ dotnet outdated ყველა ცდაზე ჩავარდა, და RestoreAfterUpdateFailed-ს, თუ ჩაწერილი
    //ვერსიებით სოლუშენი ვერ აღდგა. პაუზა არ კეთდება, რომ დანარჩენი რეპოების დამუშავება არ შეფერხდეს - ჩავარდნილი
    //რეპოების შეჯამებას UpdateOutdatedPackagesToolAction ბოლოს ბეჭდავს
    public EPackageUpdateResult RunPackageUpdate()
    {
        if (_gitIgnorePathName != CSharp)
        {
            return EPackageUpdateResult.Success;
        }

        string? solutionFileName = GetSolutionFileName();
        RestoreBeforeOutdated(solutionFileName);

        string propsFileName = Path.Combine(_projectFolderName, CentralPackageVersionsFile.FileName);
        byte[]? propsBytesBefore = CentralPackageVersionsFile.ReadBytes(propsFileName);
        bool updated = RunOutdated();
        CentralPackageVersionsFile.RestoreLostBom(propsFileName, propsBytesBefore);
        if (!updated)
        {
            return EPackageUpdateResult.OutdatedFailed;
        }

        //dotnet outdated -n ვერსიებს restore-ის შემოწმების გარეშე წერს. შემოწმება ერთხელ, სოლუშენზე კეთდება და მხოლოდ
        //მაშინ, როცა ვერსიები შეიცვალა
        if (solutionFileName is null || !CentralPackageVersionsFile.HasChanged(propsFileName, propsBytesBefore) ||
            _commands.RestoreAfterOutdated(solutionFileName).IsSuccess)
        {
            return EPackageUpdateResult.Success;
        }

        StShared.WriteWarningLine($"restore after the update failed for {solutionFileName}", true, _logger);
        return EPackageUpdateResult.RestoreAfterUpdateFailed;
    }

    private bool RunOutdated()
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            Result<(string, int)> localResult = _commands.UpdateOutdatedPackages(_projectFolderName);
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
    private void RestoreBeforeOutdated(string? solutionFileName)
    {
        if (solutionFileName is not null && _commands.RestoreForOutdated(solutionFileName).IsFailure)
        {
            StShared.WriteWarningLine($"restore before dotnet outdated failed for {solutionFileName}, continuing", true,
                _logger);
        }
    }

    //ფოლდერის ერთადერთი სოლუშენი; null, თუ ფოლდერი არ არსებობს, ან მასში სოლუშენი არ არის ან ერთზე მეტია
    private string? GetSolutionFileName()
    {
        if (!Directory.Exists(_projectFolderName))
        {
            return null;
        }

        List<string> solutionFiles = [.. Directory.EnumerateFiles(_projectFolderName).Where(IsSolutionFile)];
        return solutionFiles.Count == 1 ? solutionFiles[0] : null;
    }

    private static bool IsSolutionFile(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }
}
