using System.Net.Http;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliParametersApiClientsEdit;
using AppCliTools.CliParametersApiClientsEdit.FieldEditors;
using AppCliTools.CliParametersDataEdit.Cruders;
using AppCliTools.CliParametersEdit.Cruders;
using AppCliTools.CliParametersEdit.FieldEditors;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.SupportToolsParametersEdit;

public sealed class SupportToolsParametersEditor : ParametersEditor
{
    public SupportToolsParametersEditor(IApplication application, ILogger logger, IHttpClientFactory httpClientFactory,
        IParameters parameters, IParametersManager parametersManager) : base("Support Tools Parameters Editor",
        parameters, parametersManager)
    {
        FieldEditors.Add(new DictionaryFieldEditor<DotnetToolCruder, DotnetToolData>(
            nameof(SupportToolsParameters.DotnetTools), x => new DotnetToolCruder(parametersManager, x)));

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
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.CodeGenerateTestFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.SecurityFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.GitIgnoreFilesFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.ScaffoldSeedersWorkFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.PublisherWorkFolder)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ServiceDescriptionSignature)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.UploadTempExtension),
            SupportToolsParameters.DefaultUploadFileTempExtension));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ProgramArchiveDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ProgramArchiveExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ParametersFileDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.ParametersFileExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(SupportToolsParameters.MediatRLicenseKey)));
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
            nameof(SupportToolsParameters.ApiClients),
            x => new ApiClientCruder(logger, httpClientFactory, parametersManager, x)));

        //FieldEditors.Add(new GitsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.Gits),
        //    parametersManager));
        FieldEditors.Add(new DictionaryFieldEditor<GitCruder, GitDataModel>(nameof(SupportToolsParameters.Gits),
            x => new GitCruder(logger, httpClientFactory, parametersManager, x)));

        FieldEditors.Add(new SimpleNamesWithDescriptionsFieldEditor<ReactAppTypeCruder>(
            nameof(SupportToolsParameters.ReactAppTemplates),
            x => new ReactAppTypeCruder(logger, parametersManager, x)));

        FieldEditors.Add(
            new SimpleNamesWithDescriptionsFieldEditor<NpmPackagesCruder>(nameof(SupportToolsParameters.NpmPackages),
                x => new NpmPackagesCruder(x)));

        //FieldEditors.Add(new FileStoragesFieldEditor(logger, nameof(SupportToolsParameters.FileStorages),
        //    parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<FileStorageCruder, FileStorageData>(
            nameof(SupportToolsParameters.FileStorages), x => new FileStorageCruder(logger, parametersManager, x)));

        FieldEditors.Add(new DictionaryFieldEditor<DatabaseServerConnectionCruder, DatabaseServerConnectionData>(
            nameof(SupportToolsParameters.DatabaseServerConnections),
            x => new DatabaseServerConnectionCruder(application, logger, httpClientFactory, parametersManager, x)));

        //FieldEditors.Add(new SmartSchemasFieldEditor(nameof(SupportToolsParameters.SmartSchemas), parametersManager));
        FieldEditors.Add(new DictionaryFieldEditor<SmartSchemaCruder, SmartSchema>(
            nameof(SupportToolsParameters.SmartSchemas), x => new SmartSchemaCruder(parametersManager, x)));

        //FieldEditors.Add(new ArchiversFieldEditor(nameof(SupportToolsParameters.Archivers), parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<ArchiverCruder, ArchiverData>(
            nameof(SupportToolsParameters.Archivers), x => new ArchiverCruder(parametersManager, x)));

        //FieldEditors.Add(new ProjectsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.Projects),
        //    parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<ProjectCruder, ProjectModel>(nameof(SupportToolsParameters.Projects),
            x => new ProjectCruder(application, logger, httpClientFactory, parametersManager, x)));

        //FieldEditors.Add(new ServersFieldEditor(nameof(SupportToolsParameters.Servers), logger, httpClientFactory,
        //    parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<ServerDataCruder, ServerDataModel>(
            nameof(SupportToolsParameters.Servers),
            x => new ServerDataCruder(logger, httpClientFactory, parametersManager, x)));

        FieldEditors.Add(new SimpleNamesWithDescriptionsFieldEditor<RunTimeCruder>(
            nameof(SupportToolsParameters.RunTimes), x => new RunTimeCruder(parametersManager, x)));

        FieldEditors.Add(new SimpleNamesWithDescriptionsFieldEditor<GitIgnoreFilePathsCruder>(
            nameof(SupportToolsParameters.GitIgnoreModelFilePaths),
            x => new GitIgnoreFilePathsCruder(logger, parametersManager, x)));

        FieldEditors.Add(new SimpleNamesWithDescriptionsFieldEditor<EnvironmentCruder>(
            nameof(SupportToolsParameters.Environments), x => new EnvironmentCruder(parametersManager, x)));
    }
}
