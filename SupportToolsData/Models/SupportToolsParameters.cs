﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using ApiClientsManagement;
using CliTools;
using Installer.Models;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibFileParameters.Interfaces;
using LibFileParameters.Models;
using LibGitData;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using SupportToolsServerApiContracts;
using SystemToolsShared;

namespace SupportToolsData.Models;

public sealed class SupportToolsParameters : IParametersWithFileStorages, IParametersWithDatabaseServerConnections,
    IParametersWithSmartSchemas, IParametersWithArchivers, IParametersWithApiClients, IParametersWithRecentData
{
    public const string DefaultUploadFileTempExtension = ".up!";

    public string? SupportToolsServerWebApiClientName { get; set; }
    public string? LogFolder { get; set; }
    public bool LogGitWork { get; set; }
    public string? WorkFolder { get; set; }
    public string? FolderForGitignoreFiles { get; set; }
    public string? TempFolder { get; set; }
    public string? CodeGenerateTestFolder { get; set; }
    public string? SecurityFolder { get; set; }
    public string? ScaffoldSeedersWorkFolder { get; set; }
    public string? PublisherWorkFolder { get; set; }
    public string? ServiceDescriptionSignature { get; set; }
    public string? UploadTempExtension { get; set; }
    public string? ProgramArchiveDateMask { get; set; }
    public string? ProgramArchiveExtension { get; set; }
    public string? ParametersFileDateMask { get; set; }
    public string? ParametersFileExtension { get; set; }

    //ეს არის იმ ადგილის სახელი, სადაც უნდა მოხდეს პროგრამის დაზიპული ფაილების ატვირთვა
    public string? FileStorageNameForExchange { get; set; }
    public string? SmartSchemaNameForExchange { get; set; }
    public string? SmartSchemaNameForLocal { get; set; }
    public InstallerSettings? LocalInstallerSettings { get; set; }
    public DatabasesBackupFilesExchangeParameters? DatabasesBackupFilesExchangeParameters { get; set; }
    public AppProjectCreatorAllParameters? AppProjectCreatorAllParameters { get; set; }
    public Dictionary<string, ProjectModel> Projects { get; init; } = [];
    public Dictionary<string, ServerDataModel> Servers { get; init; } = [];
    public Dictionary<string, GitDataModel> Gits { get; init; } = [];
    public Dictionary<string, GitProjectDataModel> GitProjects { get; init; } = [];
    public Dictionary<string, string> ReactAppTemplates { get; init; } = [];
    public Dictionary<string, string> NpmPackages { get; init; } = [];
    public Dictionary<string, string> Environments { get; init; } = [];
    public Dictionary<string, string> RunTimes { get; init; } = [];
    public Dictionary<string, string> GitIgnoreModelFilePaths { get; init; } = [];
    public Dictionary<string, DotnetToolData> DotnetTools { get; init; } = [];
    public Dictionary<string, ApiClientSettings> ApiClients { get; init; } = [];
    public Dictionary<string, ArchiverData> Archivers { get; init; } = [];
    public Dictionary<string, DatabaseServerConnectionData> DatabaseServerConnections { get; init; } = [];
    public Dictionary<string, FileStorageData> FileStorages { get; init; } = [];

    public bool CheckBeforeSave()
    {
        return true;
    }

    public string? RecentCommandsFileName { get; set; }
    public int RecentCommandsCount { get; set; }

    public Dictionary<string, SmartSchema> SmartSchemas { get; init; } = [];

    public SupportToolsServerApiClient? GetSupportToolsServerApiClient(ILogger? logger,
        IHttpClientFactory httpClientFactory)
    {
        //დავადგინოთ არის თუ არა მითითებული SupportToolsServer-ის აპი კლიენტის სახელი პარამეტრებში (SupportToolsServerWebApiClientName)
        var supportToolsServerWebApiClientName = SupportToolsServerWebApiClientName;
        if (string.IsNullOrWhiteSpace(supportToolsServerWebApiClientName))
        {
            StShared.WriteErrorLine("supportToolsServerWebApiClientName does not specified", true);
            return null;
        }

        var supportToolsServerWebApiClient = GetApiClientSettingsRequired(supportToolsServerWebApiClientName);

        return new SupportToolsServerApiClient(logger, httpClientFactory, supportToolsServerWebApiClient.Server,
            supportToolsServerWebApiClient.ApiKey, true);
    }

    public string GetUploadTempExtension()
    {
        return UploadTempExtension ?? DefaultUploadFileTempExtension;
    }

    public bool DeleteGitFromProjectByNames(string projectName, string gitName, EGitCol gitCol)
    {
        var project = GetProject(projectName);
        if (project is null)
            return false;
        switch (gitCol)
        {
            case EGitCol.Main:
                project.GitProjectNames.Remove(gitName);
                return true;
            case EGitCol.ScaffoldSeed:
                project.ScaffoldSeederGitProjectNames.Remove(gitName);
                return true;
            default:
                return false;
        }
    }

    public List<string> GetGitProjectNames(string projectName, EGitCol gitCol)
    {
        var project = GetProject(projectName);
        return project is null ? [] : project.GetGitProjectNames(gitCol);
    }

    public List<string> GetNpmPackageNames(string projectName)
    {
        var project = GetProject(projectName);
        return project is null ? [] : project.FrontNpmPackageNames;
    }

    public ApiClientSettingsDomain GetApiClientSettingsRequired(string apiClientName)
    {
        var apiClientSettings = GetApiClientSettings(apiClientName) ??
                                throw new InvalidOperationException(
                                    $"ApiClient with name {apiClientName} does not exists");
        if (string.IsNullOrWhiteSpace(apiClientSettings.Server))
            throw new InvalidOperationException($"Server does not specified for ApiClient with name {apiClientName}");
        return new ApiClientSettingsDomain(apiClientSettings.Server, apiClientSettings.ApiKey);
    }

    private ApiClientSettings? GetApiClientSettings(string webAgentKey)
    {
        return ApiClients.GetValueOrDefault(webAgentKey);
    }

    public FileStorageData GetFileStorageRequired(string fileStorageName)
    {
        return GetFileStorage(fileStorageName) ??
               throw new InvalidOperationException($"FileStorage with name {fileStorageName} does not exists");
    }

    private FileStorageData? GetFileStorage(string fileStorageName)
    {
        return FileStorages.GetValueOrDefault(fileStorageName);
    }

    public SmartSchema GetSmartSchemaRequired(string smartSchemaName)
    {
        return GetSmartSchema(smartSchemaName) ??
               throw new InvalidOperationException($"SmartSchema with name {smartSchemaName} does not exists");
    }

    private SmartSchema? GetSmartSchema(string smartSchemaName)
    {
        return SmartSchemas.GetValueOrDefault(smartSchemaName);
    }

    public ServerInfoModel? GetServerByProject(string projectName, string serverName)
    {
        if (!Projects.ContainsKey(projectName))
            return null;
        var project = GetProject(projectName);
        if (project?.ServerInfos == null || !project.ServerInfos.TryGetValue(serverName, out var value))
            return null;
        return value;
    }

    public ServerDataModel GetServerDataRequired(string serverName)
    {
        return GetServerData(serverName) ??
               throw new InvalidOperationException($"server with name {serverName} is not found");
    }

    public ServerDataModel? GetServerData(string serverName)
    {
        return Servers.GetValueOrDefault(serverName);
    }

    public ProjectModel GetProjectRequired(string projectName)
    {
        return GetProject(projectName) ??
               throw new InvalidOperationException($"project with name {projectName} is not found");
    }

    public ProjectModel? GetProject(string projectName)
    {
        return Projects.GetValueOrDefault(projectName);
    }

    private string? GetProjectScaffoldSeederPath(string scaffoldSeederProjectName)
    {
        return string.IsNullOrWhiteSpace(ScaffoldSeedersWorkFolder)
            ? null
            : Path.Combine(ScaffoldSeedersWorkFolder, scaffoldSeederProjectName,
                $"{scaffoldSeederProjectName}ScaffoldSeeder");
    }

    public string? GetGitsFolder(string projectName, EGitCol gitCol)
    {
        var project = GetProject(projectName);
        if (project is null)
            return null;
        switch (gitCol)
        {
            case EGitCol.Main:
                if (!string.IsNullOrWhiteSpace(project.ProjectFolderName))
                    return project.ProjectFolderName;

                StShared.WriteErrorLine("ProjectFolderName must be specified", true);
                return null;

            case EGitCol.ScaffoldSeed:
                if (project.ScaffoldSeederProjectName != null)
                    return GetProjectScaffoldSeederPath(project.ScaffoldSeederProjectName);

                StShared.WriteErrorLine("ScaffoldSeederProjectName must be specified", true);
                return null;

            default:
                return null;
        }
    }

    public static string FixProjectGroupName(string? projectGroupName)
    {
        return string.IsNullOrWhiteSpace(projectGroupName) ? "__No Group__" : projectGroupName;
    }
}