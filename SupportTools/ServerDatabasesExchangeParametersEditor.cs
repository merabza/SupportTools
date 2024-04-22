using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsDbEdit;
using CliParametersApiClientsEdit.FieldEditors;
using CliParametersDataEdit.FieldEditors;
using CliParametersEdit.FieldEditors;
using LibDatabaseWork.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace SupportTools;

public sealed class ServerDatabasesExchangeParametersEditor : ParametersEditor
{
    public ServerDatabasesExchangeParametersEditor(ILogger logger, IParametersManager parametersManager,
        IParametersManager listsParametersManager) : base("Databases Exchange Parameters", parametersManager)
    {
        //პროდაქშენ სერვერის მხარე
        FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.ProductionDbConnectionName), listsParametersManager, true));
        FieldEditors.Add(new ApiClientNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.ProductionDbWebAgentName), listsParametersManager,
            true));
        //ბექაპირების პარამეტრები პროდაქშენ სერვერის მხარეს
        FieldEditors.Add(new DatabaseBackupParametersFieldEditor(
            nameof(DatabasesExchangeParameters.ProductionDbBackupParameters), listsParametersManager));

        //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბექაპის შენახვა
        FieldEditors.Add(new DbServerSideBackupPathFieldEditor(
            nameof(DatabasesExchangeParameters.ProductionDbServerSideBackupPath), listsParametersManager,
            nameof(DatabasesExchangeParameters.ProductionDbWebAgentName),
            nameof(DatabasesExchangeParameters.ProductionDbConnectionName)));

        //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის მონაცემების ფაილის აღდგენა
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesExchangeParameters.ProductionDbServerSideDataFolderPath)));

        //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის ლოგების ფაილის აღდგენა
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesExchangeParameters.ProductionDbServerSideLogFolderPath)));

        //ბაზის სახელის არჩევა ხდება პროდაქშენ სერვერზე არსებული ბაზების სახელებიდან.
        //შესაძლებელია ახალი სახელის მითითება, თუ პროდაქშენ ბაზა ჯერ არ არსებობს
        FieldEditors.Add(new DatabaseNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.CurrentProductionBaseName),
            listsParametersManager,
            nameof(DatabasesExchangeParameters.ProductionDbConnectionName),
            nameof(DatabasesExchangeParameters.ProductionDbWebAgentName), true));

        //ბაზის სახელის არჩევა ხდება პროდაქშენ სერვერზე არსებული ბაზების სახელებიდან.
        //შესაძლებელია ახალი სახელის მითითება, თუ პროდაქშენ ბაზა ჯერ არ არსებობს
        //NewProductionBaseName არის პროდაქშენ ბაზის სახელი, რომელიც უნდა შეიქმნას განახლების დროს
        //თუ განსხვავდება მიმდინარე სახელისგან, ეს ნიშნავს, რომ გვჭირდება მიმდინარე პროდაქშენ ბაზის შენარჩუნება
        //ხოლო ახალი პროდაქშენ ბაზისათვის მზად არის ახალი პროგრამა.
        //მას მერე, რაც საჭიროება აღარ იქნება, CurrentProductionBaseName და ProductionBaseName სახელები ერთმანეთს უნდა დაემთხვას.
        FieldEditors.Add(new DatabaseNameFieldEditor(logger, nameof(DatabasesExchangeParameters.NewProductionBaseName),
            listsParametersManager,
            nameof(DatabasesExchangeParameters.ProductionDbConnectionName),
            nameof(DatabasesExchangeParameters.ProductionDbWebAgentName), true));

        //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს პროდაქშენ ბაზის სერვერის მხარეს)
        FieldEditors.Add(new SmartSchemaNameFieldEditor(
            nameof(DatabasesExchangeParameters.ProductionSmartSchemaName), listsParametersManager));
        //პროდაქშენ სერვერის მხარეს ფაილსაცავის სახელი
        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.ProductionFileStorageName), listsParametersManager));

        //ჩამოტვირთვისა და ატვირთვის დროებითი გაფართოებები
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesExchangeParameters.DownloadTempExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesExchangeParameters.UploadTempExtension)));

        //გაცვლის ფაილსაცავის სახელი
        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.ExchangeFileStorageName), listsParametersManager));
        //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს რეზერვაციის ფაილსაცავის მხარეს)
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(DatabasesExchangeParameters.ExchangeSmartSchemaName),
            listsParametersManager));

        //ლოკალური მხარე
        FieldEditors.Add(new FolderPathFieldEditor(nameof(DatabasesExchangeParameters.LocalPath)));
        //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს ლოკალური ფოლდერის მხარეს)
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(DatabasesExchangeParameters.LocalSmartSchemaName),
            listsParametersManager));

        //დეველოპერ სერვერის მხარე
        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.DeveloperFileStorageName), listsParametersManager));
        FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.DeveloperDbConnectionName), listsParametersManager, true));
        FieldEditors.Add(new ApiClientNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.DeveloperDbWebAgentName), listsParametersManager,
            true));
        //ბექაპირების პარამეტრები დეველოპერ სერვერის მხარეს
        FieldEditors.Add(new DatabaseBackupParametersFieldEditor(
            nameof(DatabasesExchangeParameters.DeveloperDbBackupParameters), listsParametersManager));

        //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბექაპის შენახვა
        FieldEditors.Add(new DbServerSideBackupPathFieldEditor(
            nameof(DatabasesExchangeParameters.DeveloperDbServerSideBackupPath), listsParametersManager,
            nameof(DatabasesExchangeParameters.DeveloperDbWebAgentName),
            nameof(DatabasesExchangeParameters.DeveloperDbConnectionName)));

        //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის მონაცემების ფაილის აღდგენა
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesExchangeParameters.DeveloperDbServerSideDataFolderPath)));

        //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის ლოგების ფაილის აღდგენა
        FieldEditors.Add(new TextFieldEditor(nameof(DatabasesExchangeParameters.DeveloperDbServerSideLogFolderPath)));

        //პროდაქშენ ბაზის ასლის ბაზის სახელის არჩევა ხდება დეველოპერ სერვერზე არსებული ბაზების სახელებიდან
        //აქ დამატებით შეიძლება ახალი, ჯერ არ არსებული, ბაზის სახელის მითითება
        FieldEditors.Add(new DatabaseNameFieldEditor(logger,
            nameof(DatabasesExchangeParameters.ProductionBaseCopyNameForDeveloperServer), listsParametersManager,
            nameof(DatabasesExchangeParameters.DeveloperDbConnectionName),
            nameof(DatabasesExchangeParameters.DeveloperDbWebAgentName), true));
        //დეველოპერ ბაზის სახელის არჩევა ხდება დეველოპერ სერვერზე არსებული ბაზების სახელებიდან
        //აქ დამატებით შეიძლება ახალი, ჯერ არ არსებული, ბაზის სახელის მითითება
        FieldEditors.Add(new DatabaseNameFieldEditor(logger, nameof(DatabasesExchangeParameters.DeveloperBaseName),
            listsParametersManager, nameof(DatabasesExchangeParameters.DeveloperDbConnectionName),
            nameof(DatabasesExchangeParameters.DeveloperDbWebAgentName), true));

        //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს დეველოპერ ბაზის სერვერის მხარეს)
        FieldEditors.Add(new SmartSchemaNameFieldEditor(
            nameof(DatabasesExchangeParameters.DeveloperSmartSchemaName), listsParametersManager));
    }
}