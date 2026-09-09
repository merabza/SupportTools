using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

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

    public Result PublishRelease(string runtime, string outputFolderPath, string mainProjectFileName,
        string assemblyVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"publish --configuration Release --runtime {runtime} --self-contained --output {outputFolderPath} {mainProjectFileName} /p:AssemblyVersion={assemblyVersion}");
    }

    public Result CreateNewSolution(string solutionPath, string solutionName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new sln --output {solutionPath} --name {solutionName}");
    }

    public Result CreateNewProject(EDotnetProjectType dotnetProjectType, string? projectCreateParameters,
        string projectFullPath, string projectName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new {dotnetProjectType.ToString().ToLowerInvariant()}{(string.IsNullOrWhiteSpace(projectCreateParameters) ? string.Empty : $" {projectCreateParameters}")} --output {projectFullPath} --name {projectName}");
    }

    public Result AddProjectToSolution(string solutionPath, string? solutionFolderName, string projectFileFullName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"sln {solutionPath} add {(solutionFolderName is null ? string.Empty : $"--solution-folder {solutionFolderName} ")}{projectFileFullName}");
    }

    public Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} reference {referenceProjectFilePath}");
    }

    public Result AddPackageToProject(string projectFilePath, string packageName, string? packageVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} package {packageName}{(string.IsNullOrWhiteSpace(packageVersion) ? string.Empty : $" --version {packageVersion}")}");
    }

    public Result RemoveReferenceFromProject(string projectFilePath, string referenceProjectFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"remove {projectFilePath} reference {referenceProjectFilePath}");
    }

    public Result RemovePackageFromProject(string projectFilePath, string packageName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"remove {projectFilePath} package {packageName}");
    }

    public Result InitUserSecrets(string projectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"user-secrets init --project {projectFullPath}");
    }

    public Result EfDropDatabase(string dbContextName, string migrationStartupProjectFilePath,
        string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database drop --force --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Result EfAddDatabaseMigration(string migrationName, string dbContextName,
        string migrationStartupProjectFilePath, string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef migrations add \"{migrationName}\" --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Result EfUpdateDatabaseByMigration(string dbContextName, string migrationStartupProjectFilePath,
        string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database update --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Result<(string, int)> UpdateOutdatedPackagesForProjectFolder(string projectFolderName, bool useErrorLine)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"outdated -r -u {projectFolderName}", null,
            useErrorLine);
    }

    //dotnet outdated ანალიზისას თითო პროექტზე პარალელურად უშვებს "dotnet msbuild <proj> /p:NoWarn=NU1605
    // /p:TreatWarningsAsErrors=false /t:Restore,GenerateRestoreGraphFile"-ს. იმავე თვისებებით წინასწარი restore-ის შემდეგ
    //ეს გაშვებები no-op-ია და საერთო obj-ფაილებზე აღარ ეჯახება (თვისებების გარეშე restore-ისას NuGet-ის dgspec ჰეში
    //განსხვავდება და restore ხელახლა კეთდება). useErrorLine=false - ჩავარდნისას პაუზა არ კეთდება
    public Result RestoreForOutdated(string solutionFileFullName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"restore {solutionFileFullName} -p:NoWarn=NU1605 -p:TreatWarningsAsErrors=false", null, false);
    }

    public Result RunToolUsingParametersFile(string projectFilePath, string projectParametersFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"run --project {projectFilePath} --use {projectParametersFilePath}");
    }

    public Result Restore(string projectFileFullName)
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

        Result runResult = StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"build {solutionFileName}{(noIncremental ? " --no-incremental" : string.Empty)} \"-flp:LogFile={logFileName};Verbosity=quiet;Summary\"",
            null, false);
        (int errorCount, int warningCount) = ReadBuildCounts(logFileName);
        return new DotnetBuildResult(runResult.IsSuccess, errorCount, warningCount);
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
            line.AsSpan(0, line.Length - suffix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int count)
            ? count
            : null;
    }

    public Result Pack(string projectFileName, string outputFolderPath, string packageVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"pack {projectFileName} --configuration Release --output {outputFolderPath} -p:PackageVersion={packageVersion}");
    }

    public Result NugetPush(string nupkgPath, string source, string? apiKey)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"nuget push {nupkgPath} --source {source} --skip-duplicate{(string.IsNullOrWhiteSpace(apiKey) ? string.Empty : $" --api-key {apiKey}")}");
    }

    public Result EfDatabaseScaffold(string databaseScaffoldClassLibProjectFileFullName,
        string prodCopyDatabaseConnectionString, string providerPackageName,
        string createProjectSeederCodeProjectFileFullName, string dbScContextName,
        string databaseScaffoldClassLibProjectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef dbcontext scaffold --project {databaseScaffoldClassLibProjectFileFullName} \"{prodCopyDatabaseConnectionString}\" {providerPackageName} --startup-project {createProjectSeederCodeProjectFileFullName} --context {dbScContextName} --context-dir . --output-dir {Path.Combine(databaseScaffoldClassLibProjectFullPath, "Models")} --force --no-pluralize --no-onconfiguring");
    }

    public Result<(string, int)> SearchTool(string toolName)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"tool search {toolName} --take 1");
    }

    public Result<IEnumerable<string>> GetToolsRawList()
    {
        Result<(string, int)> processResult =
            StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, "tool list --global");
        if (processResult.IsFailure)
        {
            return processResult.Error;
        }

        string outputResult = processResult.Value.Item1;
        return outputResult.Split(Environment.NewLine);
    }

    //სოლუშენში შემავალი პროექტების ჩამონათვალის მიღება dotnet sln list ბრძანებით.
    //აბრუნებს პროექტების გზებს სოლუშენის ფოლდერის მიმართ
    public Result<List<string>> GetSolutionProjectsList(string solutionFileName)
    {
        Result<(string, int)> processResult =
            StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"sln {solutionFileName} list");
        if (processResult.IsFailure)
        {
            return processResult.Error;
        }

        var projects = new List<string>();
        bool headerPassed = false;
        foreach (string outputLine in processResult.Value.Item1.Split(Environment.NewLine))
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

    public Result InstallTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"tool install --global {packageId}{(string.IsNullOrEmpty(version) ? "" : $" --version {version}")}");
    }

    public Result UpdateTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"tool update --global {packageId}{(string.IsNullOrEmpty(version) ? "" : $" --version {version}")}");
    }

    /*
            var dotnetRun = StShared.RunProcess(false, null, "dotnet", $"tool {command} --global {tool.PackageId}");
     */
}
