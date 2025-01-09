using System;
using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsDbEdit;
using CliParametersApiClientsEdit.FieldEditors;
using CliParametersDataEdit.FieldEditors;
using CliParametersDataEdit.Models;
using CliParametersEdit.FieldEditors;
using DbTools;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace SupportTools.ParametersEditors;

public sealed class DatabaseParametersEditor : ParametersEditor
{
    public DatabaseParametersEditor(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, IParametersManager listsParametersManager) : base("Database Parameters",
        parametersManager)
    {
        FieldEditors.Add(
            new EnumFieldEditor<EDataProvider>(nameof(DatabasesParameters.DataProvider), EDataProvider.Sql));

        //შემდეგი 2 პარამეტრიდან გამოიყენება ერთერთი
        //ორივეს ერთდროულად შევსება არ შეიძლება.
        //ორივეს ცარელა დატოვება არ შეიძლება
        //მონაცემთა ბაზასთან კავშირის სახელი
        FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger,
            nameof(DatabasesParameters.DbConnectionName), listsParametersManager, true));

        //მონაცემთა ბაზასთან დამაკავშირებელი ვებაგენტის სახელი
        FieldEditors.Add(new ApiClientNameFieldEditor(logger, httpClientFactory,
            nameof(DatabasesParameters.DbWebAgentName), listsParametersManager, true));

        //მონაცემთა ბაზასთან დამაკავშირებელი ვებაგენტის სახელი
        FieldEditors.Add(new RemoteDbConnectionNameFieldEditor(logger, httpClientFactory,
            nameof(DatabasesParameters.RemoteDbConnectionName), listsParametersManager,
            nameof(DatabasesParameters.DbConnectionName), nameof(DatabasesParameters.DbWebAgentName)));

        //FieldEditors.Add(new EnumFieldEditor<EDataProvider>(nameof(DatabaseConnectionParameters.DataProvider),
        //    EDataProvider.Sql));

        ////ბექაპირების პარამეტრები  სერვერის მხარეს
        //FieldEditors.Add(new DatabaseBackupParametersFieldEditor(logger, nameof(DatabasesParameters.DbBackupParameters),
        //    listsParametersManager));

        //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბექაპის შენახვა
        //FieldEditors.Add(new DbServerSideBackupPathFieldEditor(nameof(DatabasesParameters.DbServerSideBackupPath),
        //    listsParametersManager, nameof(DatabasesParameters.DbWebAgentName),
        //    nameof(DatabasesParameters.DbConnectionName)));


        //public string? DbServerFoldersSetName { get; set; }
        FieldEditors.Add(new DbServerFoldersSetNameFieldEditor(logger, httpClientFactory,
            nameof(DatabasesParameters.DbServerFoldersSetName), listsParametersManager,
            nameof(DatabasesParameters.DataProvider), nameof(DatabasesParameters.DbConnectionName),
            nameof(DatabasesParameters.DbWebAgentName)));

        ////ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის მონაცემების ფაილის აღდგენა
        //FieldEditors.Add(new TextFieldEditor(nameof(DatabasesParameters.DbServerSideDataFolderPath)));

        ////ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის ლოგების ფაილის აღდგენა
        //FieldEditors.Add(new TextFieldEditor(nameof(DatabasesParameters.DbServerSideLogFolderPath)));

        ////ბაზის სახელის არჩევა ხდება სერვერზე არსებული ბაზების სახელებიდან.
        ////შესაძლებელია ახალი სახელის მითითება, თუ ბაზა ჯერ არ არსებობს
        //FieldEditors.Add(new DatabaseNameFieldEditor(logger, httpClientFactory,
        //    nameof(DatabasesParameters.CurrentBaseName), listsParametersManager,
        //    nameof(DatabasesParameters.DbConnectionName), nameof(DatabasesParameters.DbWebAgentName), true));

        //ბაზის სახელის არჩევა ხდება სერვერზე არსებული ბაზების სახელებიდან.
        //შესაძლებელია ახალი სახელის მითითება, თუ ბაზა ჯერ არ არსებობს
        //NewBaseName არის ბაზის სახელი, რომელიც უნდა შეიქმნას განახლების დროს
        //თუ განსხვავდება მიმდინარე სახელისგან, ეს ნიშნავს, რომ გვჭირდება მიმდინარე ბაზის შენარჩუნება
        //ხოლო ახალი ბაზისათვის მზად არის ახალი პროგრამა.
        //მას მერე, რაც საჭიროება აღარ იქნება, CurrentBaseName და BaseName სახელები ერთმანეთს უნდა დაემთხვას.
        FieldEditors.Add(new DatabaseNameFieldEditor(logger, httpClientFactory,
            nameof(DatabasesParameters.DatabaseName), listsParametersManager,
            nameof(DatabasesParameters.DbConnectionName), nameof(DatabasesParameters.DbWebAgentName),
            nameof(DatabasesParameters.DataProvider), true));

        //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს ბაზის სერვერის მხარეს)
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(DatabasesParameters.SmartSchemaName),
            listsParametersManager));
        //სერვერის მხარეს ფაილსაცავის სახელი
        FieldEditors.Add(new FileStorageNameFieldEditor(logger, nameof(DatabasesParameters.FileStorageName),
            listsParametersManager));

        FieldEditors.Add(new IntFieldEditor(nameof(DatabaseConnectionParameters.CommandTimeOut), 10000));
    }


    public override void CheckFieldsEnables(object record)
    {
        var dataProvider = FieldEditor.GetValue<EDataProvider>(record, nameof(DatabasesParameters.DataProvider));

        switch (dataProvider)
        {
            case EDataProvider.None:
                EnableOffAllFieldButList([nameof(DatabasesParameters.DataProvider)]);
                break;
            case EDataProvider.Sql:
                EnableOffAllFieldButList([
                    nameof(DatabasesParameters.DataProvider),
                    nameof(DatabasesParameters.DbConnectionName),
                    nameof(DatabasesParameters.DbServerFoldersSetName),
                    nameof(DatabasesParameters.DatabaseName),
                    nameof(DatabasesParameters.SmartSchemaName),
                    nameof(DatabasesParameters.FileStorageName),
                    nameof(DatabasesParameters.CommandTimeOut)
                ]);
                break;
            case EDataProvider.SqLite:
            case EDataProvider.OleDb:
                EnableOffAllFieldButList([
                    nameof(DatabasesParameters.DataProvider),
                    nameof(DatabasesParameters.DatabaseFilePath),
                    nameof(DatabasesParameters.DatabasePassword),
                    nameof(DatabasesParameters.SmartSchemaName),
                    nameof(DatabasesParameters.FileStorageName),
                    nameof(DatabasesParameters.CommandTimeOut)
                ]);
                break;
            case EDataProvider.WebAgent:
                EnableOffAllFieldButList([
                    nameof(DatabasesParameters.DataProvider),
                    nameof(DatabasesParameters.DbWebAgentName),
                    nameof(DatabasesParameters.RemoteDbConnectionName),
                    nameof(DatabasesParameters.DbServerFoldersSetName),
                    nameof(DatabasesParameters.DatabaseName),
                    nameof(DatabasesParameters.SmartSchemaName),
                    nameof(DatabasesParameters.FileStorageName),
                    nameof(DatabasesParameters.CommandTimeOut)
                ]);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}