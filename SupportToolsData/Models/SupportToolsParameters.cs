using Installer.Domain;
using Installer.Models;
using LanguageExt;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibFileParameters.Interfaces;
using LibFileParameters.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SupportToolsData.Models;

public sealed class SupportToolsParameters : IParametersWithFileStorages, IParametersWithDatabaseServerConnections,
    IParametersWithSmartSchemas, IParametersWithArchivers, IParametersWithApiClients
{
    public const string DefaultUploadFileTempExtension = ".up!";

    public string? LogFolder { get; set; }
    public string? WorkFolder { get; set; }
    public string? SecurityFolder { get; set; }
    public string? ScaffoldSeedersWorkFolder { get; set; }
    public string? PublisherWorkFolder { get; set; }
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
    public AppProjectCreatorAllParameters? AppProjectCreatorAllParameters { get; set; }
    public Dictionary<string, ProjectModel> Projects { get; init; } = new();
    public Dictionary<string, ServerDataModel> Servers { get; init; } = new();
    public Dictionary<string, string> RunTimes { get; init; } = new();
    public Dictionary<string, GitDataModel> Gits { get; init; } = new();
    public Dictionary<string, string> ReactAppTemplates { get; init; } = new();
    public Dictionary<string, GitProjectDataModel> GitProjects { get; init; } = new();
    public Dictionary<string, ApiClientSettings> ApiClients { get; set; } = new();
    public Dictionary<string, ArchiverData> Archivers { get; set; } = new();
    public Dictionary<string, DatabaseServerConnectionData> DatabaseServerConnections { get; set; } = new();
    public Dictionary<string, FileStorageData> FileStorages { get; set; } = new();
    public Dictionary<string, string> Environments { get; set; } = new();

    public bool CheckBeforeSave()
    {
        return true;
    }

    public Dictionary<string, SmartSchema> SmartSchemas { get; set; } = new();

    public string GetUploadTempExtension()
    {
        return UploadTempExtension ?? DefaultUploadFileTempExtension;
    }

    public Dictionary<string, ServerInfoModel> GetServers(string projectName)
    {
        var project = GetProject(projectName);
        return project?.ServerInfos ?? new Dictionary<string, ServerInfoModel>();
    }

    public Option<List<string>> GetGitProjectNames(string projectName, EGitCol gitCol)
    {
        var project = GetProject(projectName);
        if (project is null)
            return null;
        return gitCol switch
        {
            EGitCol.Main => project.GitProjectNames,
            EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
            _ => null
        };
    }

    public ApiClientSettingsDomain GetWebAgentRequired(string webAgentKey)
    {
        var apiClientSettings = GetWebAgent(webAgentKey);
        if (apiClientSettings is null)
            throw new InvalidOperationException($"ApiClient with name {webAgentKey} does not exists");
        //if (string.IsNullOrWhiteSpace(apiClientSettings.ApiKey))
        //    throw new InvalidOperationException($"ApiKey does not specified for ApiClient with name {webAgentKey}");
        if (string.IsNullOrWhiteSpace(apiClientSettings.Server))
            throw new InvalidOperationException($"Server does not specified for ApiClient with name {webAgentKey}");
        return new ApiClientSettingsDomain(apiClientSettings.Server, apiClientSettings.ApiKey);
    }

    private ApiClientSettings? GetWebAgent(string webAgentKey)
    {
        return !ApiClients.ContainsKey(webAgentKey) ? null : ApiClients[webAgentKey];
    }

    public FileStorageData GetFileStorageRequired(string fileStorageName)
    {
        return GetFileStorage(fileStorageName) ??
               throw new InvalidOperationException($"FileStorage with name {fileStorageName} does not exists");
    }

    private FileStorageData? GetFileStorage(string fileStorageName)
    {
        return !FileStorages.ContainsKey(fileStorageName) ? null : FileStorages[fileStorageName];
    }

    public SmartSchema GetSmartSchemaRequired(string smartSchemaName)
    {
        return GetSmartSchema(smartSchemaName) ??
               throw new InvalidOperationException($"SmartSchema with name {smartSchemaName} does not exists");
    }

    private SmartSchema? GetSmartSchema(string smartSchemaName)
    {
        return !SmartSchemas.ContainsKey(smartSchemaName) ? null : SmartSchemas[smartSchemaName];
    }

    public ServerInfoModel? GetServerByProject(string projectName, string serverName)
    {
        if (!Projects.ContainsKey(projectName))
            return null;
        var project = GetProject(projectName);
        if (project?.ServerInfos == null || !project.ServerInfos.ContainsKey(serverName))
            return null;
        return project.ServerInfos[serverName];
    }

    public ServerDataModel GetServerDataRequired(string serverName)
    {
        return GetServerData(serverName) ??
               throw new InvalidOperationException($"server with name {serverName} does not found");
    }

    public ServerDataModel? GetServerData(string serverName)
    {
        return !Servers.ContainsKey(serverName) ? null : Servers[serverName];
    }

    public ProjectModel GetProjectRequired(string projectName)
    {
        return GetProject(projectName) ??
               throw new InvalidOperationException($"project with name {projectName} does not found");
    }

    public ProjectModel? GetProject(string projectName)
    {
        return !Projects.ContainsKey(projectName) ? null : Projects[projectName];
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
        return gitCol switch
        {
            EGitCol.Main => project.ProjectFolderName,
            EGitCol.ScaffoldSeed => project.ScaffoldSeederProjectName == null
                ? null
                : GetProjectScaffoldSeederPath(project.ScaffoldSeederProjectName),
            _ => null
        };
    }
}