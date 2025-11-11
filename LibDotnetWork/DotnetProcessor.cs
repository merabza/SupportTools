using System;
using System.Collections.Generic;
using System.IO;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using SystemToolsShared.Errors;

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

    public Option<Err[]> PublishRelease(string runtime, string outputFolderPath, string mainProjectFileName,
        string assemblyVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"publish --configuration Release --runtime {runtime} --self-contained --output {outputFolderPath} {mainProjectFileName} /p:AssemblyVersion={assemblyVersion}");
    }

    public Option<Err[]> CreateNewSolution(string solutionPath, string solutionName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new sln --output {solutionPath} --name {solutionName}");
    }

    public Option<Err[]> CreateNewProject(EDotnetProjectType dotnetProjectType,
        string? projectCreateParameters, string projectFullPath, string projectName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"new {dotnetProjectType.ToString().ToLower()}{(string.IsNullOrWhiteSpace(projectCreateParameters) ? string.Empty : $" {projectCreateParameters}")} --output {projectFullPath} --name {projectName}");
    }

    public Option<Err[]> AddProjectToSolution(string solutionPath, string? solutionFolderName,
        string projectFileFullName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"sln {solutionPath} add {(solutionFolderName is null ? string.Empty : $"--solution-folder {solutionFolderName} ")}{projectFileFullName}");
    }

    public Option<Err[]> AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} reference {referenceProjectFilePath}");
    }

    public Option<Err[]> AddPackageToProject(string projectFilePath, string packageName,
        string? packageVersion)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"add {projectFilePath} package {packageName}{(string.IsNullOrWhiteSpace(packageVersion) ? string.Empty : $" --version {packageVersion}")}");
    }

    public Option<Err[]> InitUserSecrets(string projectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"user-secrets init --project {projectFullPath}");
    }

    public Option<Err[]> EfDropDatabase(string dbContextName, string migrationStartupProjectFilePath,
        string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database drop --force --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Option<Err[]> EfAddDatabaseMigration(string migrationName, string dbContextName,
        string migrationStartupProjectFilePath, string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef migrations add \"{migrationName}\" --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public Option<Err[]> EfUpdateDatabaseByMigration(string dbContextName,
        string migrationStartupProjectFilePath, string migrationProjectFileName)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef database update --context {dbContextName} --startup-project {migrationStartupProjectFilePath} --project {migrationProjectFileName}");
    }

    public OneOf<(string, int), Err[]> UpdateOutdatedPackagesForProjectFolder(string projectFolderName)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"outdated -r -u {projectFolderName}");
    }

    public Option<Err[]> RunToolUsingParametersFile(string projectFilePath, string projectParametersFilePath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"run --project {projectFilePath} --use {projectParametersFilePath}");
    }

    public Option<Err[]> EfDatabaseScaffold(string databaseScaffoldClassLibProjectFileFullName,
        string prodCopyDatabaseConnectionString, string providerPackageName,
        string createProjectSeederCodeProjectFileFullName, string dbScContextName,
        string databaseScaffoldClassLibProjectFullPath)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet,
            $"ef dbcontext scaffold --project {databaseScaffoldClassLibProjectFileFullName} \"{prodCopyDatabaseConnectionString}\" {providerPackageName} --startup-project {createProjectSeederCodeProjectFileFullName} --context {dbScContextName} --context-dir . --output-dir {Path.Combine(databaseScaffoldClassLibProjectFullPath, "Models")} --force --no-pluralize --no-onconfiguring");
    }

    public OneOf<(string, int), Err[]> SearchTool(string toolName)
    {
        return StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, $"tool search {toolName} --take 1");
    }

    public OneOf<IEnumerable<string>, Err[]> GetToolsRawList()
    {
        var processResult = StShared.RunProcessWithOutput(_useConsole, _logger, Dotnet, "tool list --global");
        if (processResult.IsT1)
            return (Err[])processResult.AsT1;
        var outputResult = processResult.AsT0.Item1;
        return outputResult.Split(Environment.NewLine);
    }

    public Option<Err[]> InstallTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"tool install --global {packageId}{(version is not null ? $" --version {version}" : "")}");
    }

    public Option<Err[]> UpdateTool(string packageId, string? version = null)
    {
        return StShared.RunProcess(_useConsole, _logger, Dotnet, $"tool update --global {packageId}{(version is not null ? $" --version {version}" : "")}");
    }

    /*
            var dotnetRun = StShared.RunProcess(false, null, "dotnet", $"tool {command} --global {tool.PackageId}");
     */
}