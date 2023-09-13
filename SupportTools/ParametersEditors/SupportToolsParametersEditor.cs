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
    public SupportToolsParametersEditor(ILogger logger, IParameters parameters,
        ParametersManager parametersManager) : base("Support Tools Parameters Editor", parameters,
        parametersManager)
    {
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.LogFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(SupportToolsParameters.WorkFolder)));
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
            nameof(SupportToolsParameters.FileStorageNameForExchange),
            parametersManager));
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(SupportToolsParameters.SmartSchemaNameForLocal),
            parametersManager));
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(SupportToolsParameters.SmartSchemaNameForExchange),
            parametersManager));
        //---
        FieldEditors.Add(new InstallerSettingsFieldEditor(logger,
            nameof(SupportToolsParameters.LocalInstallerSettings),
            parametersManager));
        FieldEditors.Add(
            new ApiClientsFieldEditor(logger, nameof(SupportToolsParameters.ApiClients), parametersManager));
        FieldEditors.Add(new GitsFieldEditor(logger, nameof(SupportToolsParameters.Gits), parametersManager));
        FieldEditors.Add(new ReactAppTemplatesFieldEditor(logger, nameof(SupportToolsParameters.ReactAppTemplates),
            parametersManager));
        FieldEditors.Add(new FileStoragesFieldEditor(logger, nameof(SupportToolsParameters.FileStorages),
            parametersManager));
        FieldEditors.Add(
            new DatabaseServerConnectionsFieldEditor(nameof(SupportToolsParameters.DatabaseServerConnections),
                parametersManager, logger));
        FieldEditors.Add(
            new SmartSchemasFieldEditor(nameof(SupportToolsParameters.SmartSchemas), parametersManager));
        FieldEditors.Add(new ArchiversFieldEditor(nameof(SupportToolsParameters.Archivers), parametersManager));
        FieldEditors.Add(
            new ProjectsFieldEditor(logger, nameof(SupportToolsParameters.Projects), parametersManager));
        FieldEditors.Add(new ServersFieldEditor(nameof(SupportToolsParameters.Servers), logger, parametersManager));
        FieldEditors.Add(new RunTimesFieldEditor(nameof(SupportToolsParameters.RunTimes), parametersManager));
        FieldEditors.Add(new EnvironmentFieldEditor(nameof(SupportToolsParameters.Environments), parametersManager));
    }
}