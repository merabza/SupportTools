using LibDatabaseParameters;
using LibParameters;

namespace SupportToolsData.Models;

public sealed class DatabasesExchangeParameters : ParametersWithStatus
{
    //პროდაქშენ სერვერის მხარე
    public string? ProductionDbConnectionName { get; set; }

    public string? ProductionDbWebAgentName { get; set; }

    ////პროდაქშენ სერვერის სახელი გამოიყენება მხოლოდ იმ შემთხვევაში თუ ვიყენებთ ApiClient-ს
    //public string? ProductionDbServerName { get; set; }

    //ბეკაპირების პარამეტრები პროდაქშენ სერვერის მხარეს
    public DatabaseBackupParametersModel? ProductionDbBackupParameters { get; set; }

    //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბექაპის შენახვა
    public string? ProductionDbServerSideBackupPath { get; set; }

    //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის მონაცემების ფაილის აღდგენა
    public string? ProductionDbServerSideDataFolderPath { get; set; }

    //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის ლოგების ფაილის აღდგენა
    public string? ProductionDbServerSideLogFolderPath { get; set; }

    //მიმდინარე პროდაქშენ ბაზის სახელი
    public string? CurrentProductionBaseName { get; set; }

    //პროდაქშენ ბაზის სახელი, რომელიც უნდა შეიქმნას განახლების დროს
    //თუ განსხვავდება მიმდინარე სახელისგან, ეს ნიშნავს, რომ გვჭირდება მიმდინარე პროდაქშენ ბაზის შენარჩუნება
    //ხოლო ახალი პროდაქშენ ბაზისათვის მზად არის ახალი პროგრამა.
    //მას მერე, რაც საჭიროება აღარ იქნება, CurrentProductionBaseName და ProductionBaseName სახელები ერთმანეთს უნდა დაემთხვას.
    public string? NewProductionBaseName { get; set; }

    //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს პროდაქშენ ბაზის სერვერის მხარეს)
    public string? ProductionSmartSchemaName { get; set; }

    //პროდაქშენ სერვერის მხარეს ფაილსაცავის სახელი
    public string? ProductionFileStorageName { get; set; }

    //ჩამოტვირთვისა და ატვირთვის დროებითი გაფართოებები
    //public string? DownloadTempExtension { get; set; }
    //public string? UploadTempExtension { get; set; }

    //გაცვლის ფაილსაცავის სახელი
    //public string? ExchangeFileStorageName { get; set; }

    //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს რეზერვაციის ფაილსაცავის მხარეს)
    //public string? ExchangeSmartSchemaName { get; set; }

    //ლოკალური ფოლდერი
    //public string? LocalPath { get; set; }

    //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს ლოკალური ფოლდერის მხარეს)
    //public string? LocalSmartSchemaName { get; set; }

    //დეველოპერ სერვერის მხარე
    public string? DeveloperFileStorageName { get; set; }
    public string? DeveloperDbConnectionName { get; set; }

    public string? DeveloperDbWebAgentName { get; set; }

    ////დეველოპერ სერვერის სახელი გამოიყენება მხოლოდ იმ შემთხვევაში თუ ვიყენებთ ApiClient-ს
    //public string? DeveloperDbServerName { get; set; }

    //ბეკაპირების პარამეტრები დეველოპერ სერვერის მხარეს
    public DatabaseBackupParametersModel? DeveloperDbBackupParameters { get; set; }

    //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბექაპის შენახვა
    public string? DeveloperDbServerSideBackupPath { get; set; }

    //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის მონაცემების ფაილის აღდგენა
    public string? DeveloperDbServerSideDataFolderPath { get; set; }

    //ფოლდერი სერვერის მხარეს, რომელშიც უნდა მოხდეს ბაზის ლოგების ფაილის აღდგენა
    public string? DeveloperDbServerSideLogFolderPath { get; set; }


    //პროდაქშენ ბაზის ასლის სახელი დეველოპერ სერვერზე დაკოპირებისას
    public string? ProductionBaseCopyNameForDeveloperServer { get; set; }
    public string? DeveloperBaseName { get; set; }

    //ჭკვიანი სქემის სახელი. გამოიყენება ძველი დასატოვებელი და წასაშლელი ფაილების განსასაზღვრად. (ეს დეველოპერ ბაზის სერვერის მხარეს)
    public string? DeveloperSmartSchemaName { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }
}