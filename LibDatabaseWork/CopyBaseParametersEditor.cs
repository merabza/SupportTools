//using CliParameters;
//using CliParameters.FieldEditors;
//using CliParameters.Models;
//using CliParametersApiClientsDbEdit;
//using CliParametersApiClientsEdit.FieldEditors;
//using CliParametersDataEdit.FieldEditors;
//using CliParametersEdit.FieldEditors;
//using Installer.AgentClients;
//using LibDatabaseWork.FieldEditors;
//using LibDatabaseWork.Models;
//using Microsoft.Extensions.Logging;

//namespace LibDatabaseWork;

//public sealed class CopyBaseParametersEditor : TaskParametersEditor
//{

//    public CopyBaseParametersEditor(ILogger logger, IParameters parameters,
//        ParametersTaskInfo parametersTaskInfo,
//        IParametersManager listsParametersManager) : base("Copy Base Parameters Editor", parameters,
//        parametersTaskInfo)
//    {
//        WebAgentClientFabric webAgentClientFabric = new WebAgentClientFabric();

//        FieldEditors.Add(new FolderPathFieldEditor(nameof(CopyBaseParameters.LogFolder)));

//        //წყარო
//        FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger,
//            nameof(CopyBaseParameters.SourceDbConnectionName), listsParametersManager, true));
//        FieldEditors.Add(new ApiClientNameFieldEditor(logger, nameof(CopyBaseParameters.SourceDbWebAgentName),
//            listsParametersManager, webAgentClientFabric, true));
//        FieldEditors.Add(new ApiClientDbServerNameFieldEditor(logger, nameof(CopyBaseParameters.SourceDbServerName),
//            listsParametersManager, webAgentClientFabric, nameof(CopyBaseParameters.SourceDbWebAgentName)));
//        FieldEditors.Add(new DatabaseBackupParametersFieldEditor(
//            nameof(CopyBaseParameters.SourceDbBackupParameters),
//            listsParametersManager));

//        FieldEditors.Add(new DbServerSideBackupPathFieldEditor(
//            nameof(CopyBaseParameters.SourceDbServerSideBackupPath),
//            listsParametersManager, nameof(CopyBaseParameters.SourceDbWebAgentName),
//            nameof(CopyBaseParameters.SourceDbServerName)));

//        //ბაზის სახელის არჩევა ხდება წყაროს შესაბამის სერვერზე არსებული ბაზების სახელებიდან
//        FieldEditors.Add(new DatabaseNameFieldEditor(logger, nameof(CopyBaseParameters.BackupBaseName),
//            listsParametersManager, webAgentClientFabric, nameof(CopyBaseParameters.SourceDbConnectionName),
//            nameof(CopyBaseParameters.SourceDbWebAgentName), nameof(CopyBaseParameters.SourceDbServerName), false));

//        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(CopyBaseParameters.SourceSmartSchemaName),
//            listsParametersManager));

//        FieldEditors.Add(new TextFieldEditor(nameof(CopyBaseParameters.DownloadTempExtension)));
//        FieldEditors.Add(new FileStorageNameFieldEditor(logger, nameof(CopyBaseParameters.SourceFileStorageName),
//            listsParametersManager));
//        //გაცვლის სერვერი
//        FieldEditors.Add(new FileStorageNameFieldEditor(logger, nameof(CopyBaseParameters.ExchangeFileStorageName),
//            listsParametersManager));
//        //ლოკალური მხარე
//        FieldEditors.Add(new FolderPathFieldEditor(nameof(CopyBaseParameters.LocalPath)));

//        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(CopyBaseParameters.LocalSmartSchemaName),
//            listsParametersManager));

//        //მიზანი
//        FieldEditors.Add(new TextFieldEditor(nameof(CopyBaseParameters.UploadTempExtension)));
//        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
//            nameof(CopyBaseParameters.DestinationFileStorageName),
//            listsParametersManager));

//        FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger,
//            nameof(CopyBaseParameters.DestinationDbConnectionName), listsParametersManager, true));
//        FieldEditors.Add(new ApiClientNameFieldEditor(logger, nameof(CopyBaseParameters.DestinationDbWebAgentName),
//            listsParametersManager, webAgentClientFabric, true));
//        FieldEditors.Add(new ApiClientDbServerNameFieldEditor(logger,
//            nameof(CopyBaseParameters.DestinationDbServerName), listsParametersManager, webAgentClientFabric,
//            nameof(CopyBaseParameters.DestinationDbWebAgentName)));

//        FieldEditors.Add(new DatabaseBackupParametersFieldEditor(
//            nameof(CopyBaseParameters.DestinationDbBackupParameters),
//            listsParametersManager));

//        FieldEditors.Add(new DbServerSideBackupPathFieldEditor(
//            nameof(CopyBaseParameters.DestinationDbServerSideBackupPath),
//            listsParametersManager, nameof(CopyBaseParameters.DestinationDbWebAgentName),
//            nameof(CopyBaseParameters.DestinationDbServerName)));

//        //ბაზის სახელის არჩევა ხდება მიზნის შესაბამის სერვერზე არსებული ბაზების სახელებიდან
//        //აქ დამატებით შეიძლება ახალი, ჯერ არ არსებული, ბაზის სახელის მითითება
//        FieldEditors.Add(new DatabaseNameFieldEditor(logger, nameof(CopyBaseParameters.DestinationBaseName),
//            listsParametersManager, webAgentClientFabric, nameof(CopyBaseParameters.DestinationDbConnectionName),
//            nameof(CopyBaseParameters.DestinationDbWebAgentName),
//            nameof(CopyBaseParameters.DestinationDbServerName), true));

//        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(CopyBaseParameters.DestinationSmartSchemaName),
//            listsParametersManager));

//        FieldEditors.Add(new ApiClientsFieldEditor(logger, nameof(CopyBaseParameters.WebAgents),
//            listsParametersManager, webAgentClientFabric));
//        FieldEditors.Add(new DatabaseServerConnectionsFieldEditor(
//            nameof(CopyBaseParameters.DatabaseServerConnections),
//            listsParametersManager, logger));
//        FieldEditors.Add(new FileStoragesFieldEditor(logger, nameof(CopyBaseParameters.FileStorages),
//            listsParametersManager));

//        FieldEditors.Add(new SmartSchemasFieldEditor(nameof(CopyBaseParameters.SmartSchemas),
//            listsParametersManager));
//    }


//    public override string GetStatusFor(string propertyName)
//    {
//        return null;
//    }

//}

