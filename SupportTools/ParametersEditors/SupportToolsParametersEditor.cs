using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsEdit.FieldEditors;
using CliParametersDataEdit.FieldEditors;
using CliParametersEdit.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.ParametersEditors;

public sealed class SupportToolsParametersEditor : ParametersEditor
{
    public SupportToolsParametersEditor(ILogger logger, IHttpClientFactory httpClientFactory, IParameters parameters,
        ParametersManager parametersManager) : base("Support Tools Parameters Editor", parameters, parametersManager)
    {
        //SupportToolsServerWebApiClientName
        FieldEditors.Add(new ApiClientNameFieldEditor(logger, httpClientFactory,
            nameof(SupportToolsParameters.SupportToolsServerWebApiClientName), parametersManager));

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
        FieldEditors.Add(new InstallerSettingsFieldEditor(logger, nameof(SupportToolsParameters.LocalInstallerSettings),
            parametersManager));
        FieldEditors.Add(new DatabasesBackupFilesExchangeParametersFieldEditor(logger,
            nameof(SupportToolsParameters.DatabasesBackupFilesExchangeParameters), parametersManager));

        //AppProjectCreatorAllParameters
        FieldEditors.Add(new ApiClientsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.ApiClients),
            parametersManager));
        FieldEditors.Add(new GitsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.Gits),
            parametersManager));
        FieldEditors.Add(new ReactAppTemplatesFieldEditor(logger, nameof(SupportToolsParameters.ReactAppTemplates),
            parametersManager));
        FieldEditors.Add(new FileStoragesFieldEditor(logger, nameof(SupportToolsParameters.FileStorages),
            parametersManager));
        FieldEditors.Add(new DatabaseServerConnectionsFieldEditor(logger, httpClientFactory, parametersManager,
            nameof(SupportToolsParameters.DatabaseServerConnections)));
        FieldEditors.Add(new SmartSchemasFieldEditor(nameof(SupportToolsParameters.SmartSchemas), parametersManager));
        FieldEditors.Add(new ArchiversFieldEditor(nameof(SupportToolsParameters.Archivers), parametersManager));
        FieldEditors.Add(new ProjectsFieldEditor(logger, httpClientFactory, nameof(SupportToolsParameters.Projects),
            parametersManager));
        FieldEditors.Add(new ServersFieldEditor(nameof(SupportToolsParameters.Servers), logger, httpClientFactory,
            parametersManager));
        FieldEditors.Add(new RunTimesFieldEditor(nameof(SupportToolsParameters.RunTimes), parametersManager));
        FieldEditors.Add(new GitIgnoreFilePathsFieldEditor(logger,
            nameof(SupportToolsParameters.GitIgnoreModelFilePaths), parametersManager));
        FieldEditors.Add(new EnvironmentFieldEditor(nameof(SupportToolsParameters.Environments), parametersManager));
    }
}