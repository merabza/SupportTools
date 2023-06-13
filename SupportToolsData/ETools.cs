namespace SupportToolsData;

public enum ETools
{
    //ეს ინსტრუმენტები გამოიყენება თითოეული პროექტისათვისა და თითოეული სერვერისათვის
    AppSettingsEncoder, //  EncodeParameters, //პარამეტრების დაშიფვრა

    // EncodeParameters=>GenerateEncodedParametersFile=>UploadParametersToExchange
    AppSettingsInstaller, //  InstallParameters, //დაშიფრული პარამეტრების განახლება

    //(DownloadParameters=>StopProgramIfRunning=>UpdateParameters=>StartServiceIfRequired)
    AppSettingsUpdater, //  UpdateParameters, //პარამეტრების დაშიფვრა და დაინსტალირებული პროგრამისთვის ამ დაშიფრული პარამეტრების გადაგზავნა-განახლება

    //EncodeParameters=>GenerateEncodedParametersFile=>UploadParametersToExchange=>
    //(RunAppSettingsInstaller)(DownloadParameters=>StopProgramIfRunning=>UpdateParameters=>StartServiceIfRequired)
    ProgPublisher, //  PublishProgram, //პროგრამის საინსტალაციო პაკეტის გამზადება

    //CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters
    ProgramInstaller, //  InstallProgram, //პროგრამის საინსტალაციო პაკეტის გამოყენებით პროგრამის დაინსტალირება-განახლება

    //DownloadPackage=>DownloadParameters=>StopProgramIfRunning=>UpdateProgram=>UpdateParameters=>StartServiceIfRequired
    ProgramUpdater, //  PublishAndInstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამზადება და პროგრამის დაინსტალირება-განახლება

    //CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters=>
    //(RunProgramInstaller)(DownloadPackage=>DownloadParameters=>StopProgramIfRunning=>UpdateProgram=>UpdateParameters=>StartServiceIfRequired)
    ProgRemover, //  RemoveProgram, //პროგრამის წაშლა
    ServiceStarter, //უკვე დაინსტალირებული სერვისის გაშვება სერვერის მხარეს.
    ServiceStopper, //გაშვებული სერვისის გაჩერება სერვერის მხარეს.
    VersionChecker, //გაშვებული სერვისის ვერსიის დადგენა. (ეს იმუშავებს მხოლოდ იმ შემთხვევაში, თუ სერვისში გამოყენებული იქნება იქნება TestTools.Controllers.TestController
    ProdBaseToDevCopier, //ბაზის დაკოპირება პროდაქშენ სერვერიდან დეველოპერ სერვერზე.
    DevBaseToProdCopier, //ბაზის დაკოპირება დეველოპერ სერვერიდან პროდაქშენ სერვერზე.

    //პროექტზე დამოკიდებული ინსტრუმენტები. (გამოიყენება დეველოპმენტის დროს და არ არის დამოკიდებული სერვერზე
    RecreateDevDatabase, //აერთიანებს წინა სამ ოპერაციას, ანუ წაშლის დეველოპერ ბაზას, შექმნის ახალ ცარელა ბაზას მიგრაციის მიხედვით და დააკორექტირებს არასასურველ კონსტრუქციებს ბაზაში.
    DropDevDatabase, //დეველოპერ ბაზის წაშლა
    CreateDevDatabaseByMigration, //მიგრაციის საშუალებით ცარელა დეველოპერ ბაზის შექმნა
    CorrectNewDatabase, //მიგრაციით ახლად შექმნილი მონაცემთა ბაზის კორექტირება, იმისათვის რომ ზოგიერთი არასასურველი კონსტრუქცია შეიცვალოს ისე, როგორც საჭიროა

    ////ცალკე მდგომი ინსტრუმენტები, რომლებიც შეიძლება არ იყვნენ დამოკიდებული პროექტსა და სერვერზე
    //BaseCopier, //ბაზის დაკოპირება ან სერვერიდან ლოკალურად, ან პირიქით
    //AppProjectCreator, //კონსოლაპლიკაციის შექმნა

    //ScaffoldSeederGitSync, //მხოლოდ გიტის სინქრონიზაცია სკაფოლდ სიდინგ პროექტებისათვის
    ScaffoldSeederCreator, //არსებული პროდაქშენ ბაზის ასლიდან დაამზადებს სკაფოლდ პროექტს და შექმნის ჯეისონ ფაილებს, რომლებიც შემდგომში შესაძლებელი იქნება გამოვიყენოთ სიდინგისათვის. ამ ქრეათორის ამოცანაში შედის კოდის შექმნა, როგორც სკაფოლდინგისათვის, ისე სიდინგისათვის
    JsonFromProjectDbProjectGetter, //არსებული პროდაქშენ ბაზის ასლიდან დაამზადებს json ფაილები თავიდან

    SeedData, //scaffold-ით მიღებული ბაზიდან მიღებულ json-ებზე დაყრდნობით ინფორმაციის ჩაყრა დეველოპერ ბაზაში.
    JetBrainsCleanupCode //jb cleanupcode solutionFileName.sln -> JetBrain-ის უტილიტის გაშვება პროექტის სოლუშენის ფაილის მითითებით კოდის გასაწმენდად და მოსაწესრიგებლად

    //SupportFilesCreator, //Suppor ფაილების შექმნა, რომლითაც 
}

//public enum EProgramManipulationType
//{
//  EncodeParameters, //პარამეტრების დაშიფვრა
//  UpdateParameters, //პარამეტრების დაშიფვრა და დაინსტალირებული პროგრამისთვის ამ დაშიფრული პარამეტრების გადაგზავნა-განახლება
//  Publish, //პროგრამის საინსტალაციო პაკეტის გამზადება
//  InstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამოყენებით პროგრამის დაინსტალირება-განახლება
//  PublishAndInstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამზადება და პროგრამის დაინსტალირება-განახლება
//  Remove, //პროგრამის წაშლა
//}