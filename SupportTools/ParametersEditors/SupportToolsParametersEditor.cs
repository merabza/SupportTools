using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsEdit;
using CliParametersApiClientsEdit.FieldEditors;
using CliParametersDataEdit.Cruders;
using CliParametersEdit.Cruders;
using CliParametersEdit.FieldEditors;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibFileParameters.Models;
using LibGitData.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.ParametersEditors;

public sealed class SupportToolsParametersEditor : ParametersEditor
{
    public SupportToolsParametersEditor(ILogger logger, IHttpClientFactory httpClientFactory, IParameters parameters,
        ParametersManager parametersManager) : base("Support Tools Parameters Editor", parameters, parametersManager)
    {

        FieldEditors.Add(
            new DictionaryFieldEditor<DotnetToolCruder, DotnetToolData>(nameof(SupportToolsParameters.DotnetTools),
                parametersManager));

        //SupportToolsServerWebApiClientName
        FieldEditors.Add(new ApiClientNameFieldEditor(nameof(SupportToolsParameters.SupportToolsServerWebApiClientName),
            logger, httpClientFactory, parametersManager));

        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.LogFolder)));
        FieldEditors.Add(new BoolFieldEditor(nameof(SupportToolsParameters.LogGitWork)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.WorkFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.FolderForGitignoreFiles)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(SupportToolsParameters.RecentCommandsFileName)));
        FieldEditors.Add(new IntFieldEditor(nameof(SupportToolsParameters.RecentCommandsCount)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.TempFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.SecurityFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.ScaffoldSeedersWorkFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.PublisherWorkFolder)));

        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ServiceDescriptionSignature)));

        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.UploadTempExtension),
            SupportToolsParameters.DefaultUploadFileTempExtension));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ProgramArchiveDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ProgramArchiveExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ParametersFileDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ParametersFileExtension)));
        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
            nameof(SupportToolsParameters.FileStorageNameForExchange), parametersManager));
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(SupportToolsParameters.SmartSchemaNameForLocal),
            parametersManager));
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(SupportToolsParameters.SmartSchemaNameForExchange),
            parametersManager));
        //---
        FieldEditors.Add(new InstallerSettingsFieldEditor(nameof(SupportToolsParameters.LocalInstallerSettings), logger,
            parametersManager));
        FieldEditors.Add(new DatabasesBackupFilesExchangeParametersFieldEditor(
            nameof(SupportToolsParameters.DatabasesBackupFilesExchangeParameters), logger, parametersManager));

        //AppProjectCreatorAllParameters
        //FieldEditors.Add(new ApiClientsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.ApiClients),
        //    parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<ApiClientCruder, ApiClientSettings>(
            nameof(SupportToolsParameters.ApiClients), logger, httpClientFactory, parametersManager));

        //FieldEditors.Add(new GitsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.Gits),
        //    parametersManager));
        FieldEditors.Add(new DictionaryFieldEditor<GitCruder, GitDataModel>(nameof(SupportToolsParameters.Gits), logger,
            httpClientFactory, parametersManager));

        FieldEditors.Add(new SimpleNamesWithDescriptionsFieldEditor<ReactAppTypeCruder>(logger,
            nameof(SupportToolsParameters.ReactAppTemplates), parametersManager));
        FieldEditors.Add(
            new SimpleNamesWithDescriptionsFieldEditor<NpmPackagesCruder>(nameof(SupportToolsParameters.NpmPackages),
                parametersManager));

        //FieldEditors.Add(new FileStoragesFieldEditor(logger, nameof(SupportToolsParameters.FileStorages),
        //    parametersManager));

        FieldEditors.Add(
            new DictionaryFieldEditor<FileStorageCruder, FileStorageData>(nameof(SupportToolsParameters.FileStorages),
                logger, parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<DatabaseServerConnectionCruder, DatabaseServerConnectionData>(
            nameof(SupportToolsParameters.DatabaseServerConnections), logger, httpClientFactory, parametersManager));

        //FieldEditors.Add(new SmartSchemasFieldEditor(nameof(SupportToolsParameters.SmartSchemas), parametersManager));
        FieldEditors.Add(
            new DictionaryFieldEditor<SmartSchemaCruder, SmartSchema>(nameof(SupportToolsParameters.SmartSchemas),
                parametersManager));

        //FieldEditors.Add(new ArchiversFieldEditor(nameof(SupportToolsParameters.Archivers), parametersManager));

        FieldEditors.Add(
            new DictionaryFieldEditor<ArchiverCruder, ArchiverData>(nameof(SupportToolsParameters.Archivers),
                parametersManager));

        //FieldEditors.Add(new ProjectsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.Projects),
        //    parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<ProjectCruder, ProjectModel>(nameof(SupportToolsParameters.Projects),
            logger, httpClientFactory, parametersManager));

        //FieldEditors.Add(new ServersFieldEditor(nameof(SupportToolsParameters.Servers), logger, httpClientFactory,
        //    parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<ServerDataCruder, ServerDataModel>(
            nameof(SupportToolsParameters.Servers), logger, httpClientFactory, parametersManager));

        FieldEditors.Add(
            new SimpleNamesWithDescriptionsFieldEditor<RunTimeCruder>(nameof(SupportToolsParameters.RunTimes),
                parametersManager));
        FieldEditors.Add(new SimpleNamesWithDescriptionsFieldEditor<GitIgnoreFilePathsCruder>(logger,
            nameof(SupportToolsParameters.GitIgnoreModelFilePaths), parametersManager));
        FieldEditors.Add(
            new SimpleNamesWithDescriptionsFieldEditor<EnvironmentCruder>(nameof(SupportToolsParameters.Environments),
                parametersManager));
    }
}