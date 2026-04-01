using System;
using System.Collections.Generic;
using System.IO;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace LibDotnetWork;

public sealed class DotnetProcessor
{
    private const string Dotnet = "dotnet";
    private readonly ILogger? _logger;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetProcessor(ILogger? logger, bool useConsole)
    {
        _logger = logger;
        _useConsole = useConsole;
    }

    public Option<Error[]> PublishRelease(string runtime, string outputFolderPath, string mainProjectFileName,
        string assemblyVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"publish --configuration Release --runtime {runtime} --self-contained --output {outputFolderPath} {mainProjectFileName} /p:AssemblyVersion={assemblyVersion}");
    }

    public Option<Error[]> CreateNewSolution(string solutionPath, string solutionName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new sln --output {solutionPath} --name {solutionName}");
    }

    public Option<Error[]> CreateNewProject(EDotnetProjectType dotnetProjectType, string? projectCreateParameters,
        string projectFullPath, string projectName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new {dotnetProjectType.ToString().ToLowerInvariant()}{(string.IsNullOrWhiteSpace(projectCreateParameters) ? string.Empty : $" {projectCreateParameters}")} --output {projectFullPath} --name {projectName}");
    }

    public Option<Error[]> AddProjectToSolution(string solutionPath, string? solutionFolderName,
        string projectFileFullName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"sln {solutionPath} add {(solutionFolderName is null ? string.Empty : $"--solution-folder {solutionFolderName} ")}{projectFileFullName}");
    }

    public Option<Error[]> AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} reference {referenceProjectFilePath}");
    }

    public Option<Error[]> AddPackageToProject(string projectFilePath, string packageName, string? packageVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} package {packageName}{(string.IsNullOrWhiteSpace(packageVersion) ? string.Empty : $" --version {packageVersion}")}");
    }

    public Option<Error[]> InitUserSecrets(string projectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"user-secrets init --project {projectFullPath}");
    }

    public Option<Error[]> EfDropDatabase(string dbContextName, string migrationStartupProjectFilePath,
        string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database drop --force --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Option<Error[]> EfAddDatabaseMigration(string migrationName, string dbContextName,
        string migrationStartupProjectFilePath, string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef migrations add \"{migrationName}\" --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Option<Error[]> EfUpdateDatabaseByMigration(string dbContextName, string migrationStartupProjectFilePath,
        string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database update --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public OneOf<(string, int), Error[]> UpdateOutdatedPackagesForProjectFolder(string projectFolderName)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"outdated -r -u {projectFolderName}");
    }

    public Option<Error[]> RunToolUsingParametersFile(string projectFilePath, string projectParametersFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"run --project {projectFilePath} --use {projectParametersFilePath}");
    }

    public Option<Error[]> Restore(string projectFileFullName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"restore {projectFileFullName}");
    }

    public Option<Error[]> EfDatabaseScaffold(string databaseScaffoldClassLibProjectFileFullName,
        string prodCopyDatabaseConnectionString, string providerPackageName,
        string createProjectSeederCodeProjectFileFullName, string dbScContextName,
        string databaseScaffoldClassLibProjectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef dbcontext scaffold --project {databaseScaffoldClassLibProjectFileFullName} \"{prodCopyDatabaseConnectionString}\" {providerPackageName} --startup-project {createProjectSeederCodeProjectFileFullName} --context {dbScContextName} --context-dir . --output-dir {Path.Combine(databaseScaffoldClassLibProjectFullPath, "Models")} --force --no-pluralize --no-onconfiguring");
    }

    public OneOf<(string, int), Error[]> SearchTool(string toolName)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"tool search {toolName} --take 1");
    }

    public OneOf<IEnumerable<string>, Error[]> GetToolsRawList()
    {
        OneOf<(string, int), Error[]> processResult =
            StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, "tool list --global");
        if (processResult.IsT1)
        {
            return processResult.AsT1;
        }

        string outputResult = processResult.AsT0.Item1;
        return outputResult.Split(Environment.NewLine);
    }

    public Option<Error[]> InstallTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"tool install --global {packageId}{(string.IsNullOrEmpty(version) ? "" : $" --version {version}")}");
    }

    public Option<Error[]> UpdateTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"tool update --global {packageId}{(string.IsNullOrEmpty(version) ? "" : $" --version {version}")}");
    }

    /*
            var dotnetRun = StShared.RunProcess(false, null, "dotnet", $"tool {command} --global {tool.PackageId}");
     */
}
