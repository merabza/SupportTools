using CliParameters;
using CliParameters.FieldEditors;
using CliParametersEdit.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;

namespace SupportTools.ParametersEditors;

public sealed class DatabasesBackupFilesExchangeParametersEditor : ParametersEditor
{
    public DatabasesBackupFilesExchangeParametersEditor(ILogger logger, IParameters parameters,
        IParametersManager parametersManager) : base("Databases Backup Files Exchange Parameters Editor", parameters,
        parametersManager)
    {
        //ჩამოტვირთვისა და ატვირთვის დროებითი გაფართოებები
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesBackupFilesExchangeParameters.DownloadTempExtension),
            DatabasesBackupFilesExchangeParameters.DefaultDownloadTempExtension));
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesBackupFilesExchangeParameters.UploadTempExtension),
            DatabasesBackupFilesExchangeParameters.DefaultUploadTempExtension));

        //გაცვლის ფაილსაცავის სახელი
        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
            nameof(DatabasesBackupFilesExchangeParameters.ExchangeFileStorageName), parametersManager));
        //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს რეზერვაციის ფაილსაცავის მხარეს)
        FieldEditors.Add(new SmartSchemaNameFieldEditor(
            nameof(DatabasesBackupFilesExchangeParameters.ExchangeSmartSchemaName), parametersManager));

        //ლოკალური მხარე
        FieldEditors.Add(new FolderPathFieldEditor(nameof(DatabasesBackupFilesExchangeParameters.LocalPath)));
        //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს ლოკალური ფოლდერის მხარეს)
        FieldEditors.Add(new SmartSchemaNameFieldEditor(
            nameof(DatabasesBackupFilesExchangeParameters.LocalSmartSchemaName), parametersManager));
    }
}