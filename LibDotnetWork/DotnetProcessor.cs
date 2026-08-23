using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace LibDotnetWork;

public sealed class DotnetProcessor
{
    private const string Dotnet = "dotnet";
    private const string WarningCountSuffix = " Warning(s)";
    private const string ErrorCountSuffix = " Error(s)";
    private readonly ILogger? _logger;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetProcessor(ILogger? logger, bool useConsole)
    {
        _logger = logger;
        _useConsole = useConsole;
    }

    public Option<ErrorOmd[]> PublishRelease(string runtime, string outputFolderPath, string mainProjectFileName,
        string assemblyVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"publish --configuration Release --runtime {runtime} --self-contained --output {outputFolderPath} {mainProjectFileName} /p:AssemblyVersion={assemblyVersion}");
    }

    public Option<ErrorOmd[]> CreateNewSolution(string solutionPath, string solutionName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new sln --output {solutionPath} --name {solutionName}");
    }

    public Option<ErrorOmd[]> CreateNewProject(EDotnetProjectType dotnetProjectType, string? projectCreateParameters,
        string projectFullPath, string projectName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new {dotnetProjectType.ToString().ToLowerInvariant()}{(string.IsNullOrWhiteSpace(projectCreateParameters) ? string.Empty : $" {projectCreateParameters}")} --output {projectFullPath} --name {projectName}");
    }

    public Option<ErrorOmd[]> AddProjectToSolution(string solutionPath, string? solutionFolderName,
        string projectFileFullName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"sln {solutionPath} add {(solutionFolderName is null ? string.Empty : $"--solution-folder {solutionFolderName} ")}{projectFileFullName}");
    }

    public Option<ErrorOmd[]> AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} reference {referenceProjectFilePath}");
    }

    public Option<ErrorOmd[]> AddPackageToProject(string projectFilePath, string packageName, string? packageVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} package {packageName}{(string.IsNullOrWhiteSpace(packageVersion) ? string.Empty : $" --version {packageVersion}")}");
    }

    public Option<ErrorOmd[]> RemoveReferenceFromProject(string projectFilePath, string referenceProjectFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"remove {projectFilePath} reference {referenceProjectFilePath}");
    }

    public Option<ErrorOmd[]> RemovePackageFromProject(string projectFilePath, string packageName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"remove {projectFilePath} package {packageName}");
    }

    public Option<ErrorOmd[]> InitUserSecrets(string projectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"user-secrets init --project {projectFullPath}");
    }

    public Option<ErrorOmd[]> EfDropDatabase(string dbContextName, string migrationStartupProjectFilePath,
        string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database drop --force --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Option<ErrorOmd[]> EfAddDatabaseMigration(string migrationName, string dbContextName,
        string migrationStartupProjectFilePath, string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef migrations add \"{migrationName}\" --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Option<ErrorOmd[]> EfUpdateDatabaseByMigration(string dbContextName, string migrationStartupProjectFilePath,
        string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database update --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public OneOf<(string, int), ErrorOmd[]> UpdateOutdatedPackagesForProjectFolder(string projectFolderName)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"outdated -r -u {projectFolderName}");
    }

    public Option<ErrorOmd[]> RunToolUsingParametersFile(string projectFilePath, string projectParametersFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"run --project {projectFilePath} --use {projectParametersFilePath}");
    }

    public Option<ErrorOmd[]> Restore(string projectFileFullName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"restore {projectFileFullName}");
    }

    //useErrorLine=false, რათა მრავალ პროექტზე ციკლში გაშვებისას ყოველ წარუმატებელ build-ზე არ შეჩერდეს.
    //შეცდომებისა და გაფრთხილებების რაოდენობა იკითხება MSBuild-ის ფაილური ლოგერის (-flp) შეჯამებიდან და არა
    //კონსოლიდან, რათა კონსოლში build-ის გამოტანა უცვლელი დარჩეს. noIncremental (--no-incremental) ზუსტი დათვლისთვისაა -
    //მის გარეშე up-to-date პროექტებზე კომპილაცია არ ეშვება და გაფრთხილებები არ ითვლება
    public DotnetBuildResult Build(string solutionFileName, bool noIncremental)
    {
        string logFolderPath = Path.Combine(Path.GetTempPath(), "SupportTools", "BuildLogs");
        Directory.CreateDirectory(logFolderPath);
        string logFileName = Path.Combine(logFolderPath, "LastBuild.log");
        //წინა build-ის ლოგი არ უნდა წაიკითხოს, თუ MSBuild ამჯერად ფაილს ვერ შექმნის
        File.Delete(logFileName);

        Option<ErrorOmd[]> runResult = StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"build {solutionFileName}{(noIncremental ? " --no-incremental" : string.Empty)} \"-flp:LogFile={logFileName};Verbosity=quiet;Summary\"",
            null, false);
        (int errorCount, int warningCount) = ReadBuildCounts(logFileName);
        return new DotnetBuildResult(runResult.IsNone, errorCount, warningCount);
    }

    //MSBuild-ის შეჯამების ხაზები: "    N Warning(s)" და "    N Error(s)". ბოლო შეხვედრა იგებს.
    //ლოგის არარსებობისას - ნულები
    private static (int ErrorCount, int WarningCount) ReadBuildCounts(string logFileName)
    {
        int errorCount = 0;
        int warningCount = 0;
        if (!File.Exists(logFileName))
        {
            return (errorCount, warningCount);
        }

        foreach (string line in File.ReadLines(logFileName))
        {
            string trimmedLine = line.Trim();
            warningCount = ParseCount(trimmedLine, WarningCountSuffix) ?? warningCount;
            errorCount = ParseCount(trimmedLine, ErrorCountSuffix) ?? errorCount;
        }

        return (errorCount, warningCount);
    }

    private static int? ParseCount(string line, string suffix)
    {
        return line.EndsWith(suffix, StringComparison.Ordinal) && int.TryParse(
            line.AsSpan(0, line.Length - suffix.Length), NumberStyles.None, CultureInfo.InvariantCulture,
            out int count)
            ? count
            : null;
    }

    public Option<ErrorOmd[]> Pack(string projectFileName, string outputFolderPath, string packageVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"pack {projectFileName} --configuration Release --output {outputFolderPath} -p:PackageVersion={packageVersion}");
    }

    public Option<ErrorOmd[]> NugetPush(string nupkgPath, string source, string? apiKey)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"nuget push {nupkgPath} --source {source} --skip-duplicate{(string.IsNullOrWhiteSpace(apiKey) ? string.Empty : $" --api-key {apiKey}")}");
    }

    public Option<ErrorOmd[]> EfDatabaseScaffold(string databaseScaffoldClassLibProjectFileFullName,
        string prodCopyDatabaseConnectionString, string providerPackageName,
        string createProjectSeederCodeProjectFileFullName, string dbScContextName,
        string databaseScaffoldClassLibProjectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef dbcontext scaffold --project {databaseScaffoldClassLibProjectFileFullName} \"{prodCopyDatabaseConnectionString}\" {providerPackageName} --startup-project {createProjectSeederCodeProjectFileFullName} --context {dbScContextName} --context-dir . --output-dir {Path.Combine(databaseScaffoldClassLibProjectFullPath, "Models")} --force --no-pluralize --no-onconfiguring");
    }

    public OneOf<(string, int), ErrorOmd[]> SearchTool(string toolName)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"tool search {toolName} --take 1");
    }

    public OneOf<IEnumerable<string>, ErrorOmd[]> GetToolsRawList()
    {
        OneOf<(string, int), ErrorOmd[]> processResult =
            StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, "tool list --global");
        if (processResult.IsT1)
        {
            return processResult.AsT1;
        }

        string outputResult = processResult.AsT0.Item1;
        return outputResult.Split(Environment.NewLine);
    }

    //სოლუშენში შემავალი პროექტების ჩამონათვალის მიღება dotnet sln list ბრძანებით.
    //აბრუნებს პროექტების გზებს სოლუშენის ფოლდერის მიმართ
    public OneOf<List<string>, ErrorOmd[]> GetSolutionProjectsList(string solutionFileName)
    {
        OneOf<(string, int), ErrorOmd[]> processResult =
            StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"sln {solutionFileName} list");
        if (processResult.IsT1)
        {
            return processResult.AsT1;
        }

        var projects = new List<string>();
        bool headerPassed = false;
        foreach (string outputLine in processResult.AsT0.Item1.Split(Environment.NewLine))
        {
            string trimmedLine = outputLine.Trim();
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            //სათაურის ხაზები მთავრდება ტირეებისგან შემდგარი გამყოფი ხაზით
            if (!headerPassed)
            {
                headerPassed = trimmedLine.All(c => c == '-');
                continue;
            }

            projects.Add(trimmedLine);
        }

        return projects;
    }

    public Option<ErrorOmd[]> InstallTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"tool install --global {packageId}{(string.IsNullOrEmpty(version) ? "" : $" --version {version}")}");
    }

    public Option<ErrorOmd[]> UpdateTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"tool update --global {packageId}{(string.IsNullOrEmpty(version) ? "" : $" --version {version}")}");
    }

    /*
            var dotnetRun = StShared.RunProcess(false, null, "dotnet", $"tool {command} --global {tool.PackageId}");
     */
}
