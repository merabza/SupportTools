using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly string _projectFolderName;

    private PackageUpdaterToolAction(ILogger? logger, GitSyncParameters gitSyncParameters) : this(logger,
        Path.Combine(gitSyncParameters.GitsFolder, gitSyncParameters.GitData.GitProjectFolderName),
        gitSyncParameters.GitData.GitIgnorePatternName, new DotnetPackageUpdaterCommands())
    {
    }

    //dotnet-ის ბრძანებები პარამეტრადაა გამოტანილი, რომ ტესტებმა restore და dotnet outdated არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal PackageUpdaterToolAction(ILogger? logger, string projectFolderName, string gitIgnorePathName,
        IPackageUpdaterCommands commands) : base(logger, "Git Sync", null, null)
    {
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
    //ვერსიებით სოლუშენი ვერ აღდგა. ნაბიჯები და შეტყობინებები output-ში იწერება: git-ები პარალელურად ახლდება, ამიტომ
    //კონსოლზე პირდაპირ არაფერი გადის. ჩავარდნილი რეპოების შეჯამებას UpdateOutdatedPackagesToolAction ბოლოს ბეჭდავს
    internal EPackageUpdateResult RunPackageUpdate(IPackageUpdateOutput output)
    {
        if (_gitIgnorePathName != CSharp)
        {
            return EPackageUpdateResult.Success;
        }

        string? solutionFileName = GetSolutionFileName();
        RestoreBeforeOutdated(solutionFileName, output);

        string propsFileName = Path.Combine(_projectFolderName, CentralPackageVersionsFile.FileName);
        byte[]? propsBytesBefore = CentralPackageVersionsFile.ReadBytes(propsFileName);
        bool updated = RunOutdated(output);
        CentralPackageVersionsFile.RestoreLostBom(propsFileName, propsBytesBefore);
        if (!updated)
        {
            return EPackageUpdateResult.OutdatedFailed;
        }

        if (!CentralPackageVersionsFile.HasChanged(propsFileName, propsBytesBefore))
        {
            return EPackageUpdateResult.Success;
        }

        int changedVersionCount = CentralPackageVersionsFile.CountChangedVersions(propsBytesBefore,
            CentralPackageVersionsFile.ReadBytes(propsFileName));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{changedVersionCount} package {(changedVersionCount == 1 ? "version" : "versions")} updated"), null);

        //dotnet outdated -n ვერსიებს restore-ის შემოწმების გარეშე წერს. შემოწმება ერთხელ, სოლუშენზე კეთდება და მხოლოდ
        //მაშინ, როცა ვერსიები შეიცვალა
        if (solutionFileName is null)
        {
            return EPackageUpdateResult.Success;
        }

        output.SetStatus("check restore");
        Result restoreResult = _commands.RestoreAfterOutdated(solutionFileName);
        if (restoreResult.IsSuccess)
        {
            return EPackageUpdateResult.Success;
        }

        output.WriteLine(
            $"restore after the update failed for {solutionFileName}{Environment.NewLine}{restoreResult.Error.Description}",
            ConsoleColor.Red);
        return EPackageUpdateResult.RestoreAfterUpdateFailed;
    }

    private bool RunOutdated(IPackageUpdateOutput output)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            output.SetStatus(attempt == 1
                ? "dotnet outdated"
                : string.Create(CultureInfo.InvariantCulture, $"dotnet outdated, attempt {attempt} of {MaxAttempts}"));
            Result result = _commands.UpdateOutdatedPackages(_projectFolderName);
            if (result.IsSuccess)
            {
                return true;
            }

            //ბოლო ცდამდე ეს გაფრთხილებაა: შემდეგი ცდა თითქმის ყოველთვის გადის
            output.WriteLine(
                $"dotnet outdated failed for {_projectFolderName} (attempt {attempt} of {MaxAttempts}){Environment.NewLine}{result.Error.Description}",
                attempt < MaxAttempts ? ConsoleColor.Yellow : ConsoleColor.Red);
        }

        return false;
    }

    //dotnet outdated პროექტებს პარალელურად აანალიზებს და თითოეულზე რეკურსიულ restore-ს უშვებს, რომლებიც საერთო
    //obj-ფაილებზე ერთმანეთს ეჯახება ("Cannot create a file when that file already exists"). წინასწარი restore ამ
    //გაშვებებს no-op-ად აქცევს. კეთდება მხოლოდ მაშინ, როცა ფოლდერში ზუსტად ერთი სოლუშენია; ჩავარდნა არ აჩერებს,
    //dotnet outdated მაინც გაეშვება
    private void RestoreBeforeOutdated(string? solutionFileName, IPackageUpdateOutput output)
    {
        if (solutionFileName is null)
        {
            return;
        }

        output.SetStatus("restore");
        Result restoreResult = _commands.RestoreForOutdated(solutionFileName);
        if (restoreResult.IsFailure)
        {
            output.WriteLine(
                $"restore before dotnet outdated failed for {solutionFileName}, continuing{Environment.NewLine}{restoreResult.Error.Description}",
                ConsoleColor.Yellow);
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
