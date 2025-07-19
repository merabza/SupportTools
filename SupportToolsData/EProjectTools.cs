namespace SupportToolsData;

public enum EProjectTools
{
    //-------------------------------------------------------------------------------
    //პროექტზე დამოკიდებული ინსტრუმენტები. (გამოიყენება დეველოპმენტის დროს და არ არის დამოკიდებული სერვერზე
    CorrectNewDatabase, //მიგრაციით ახლად შექმნილი მონაცემთა ბაზის კორექტირება, იმისათვის რომ ზოგიერთი არასასურველი კონსტრუქცია შეიცვალოს ისე, როგორც საჭიროა

    CreateDevDatabaseByMigration, //მიგრაციის საშუალებით ცარელა დეველოპერ ბაზის შექმნა
    RecreateDevDatabase, //აერთიანებს წინა სამ ოპერაციას, ანუ წაშლის დეველოპერ ბაზას, შექმნის ახალ ცარელა ბაზას მიგრაციის მიხედვით და დააკორექტირებს არასასურველ კონსტრუქციებს ბაზაში.

    DropDevDatabase, //დეველოპერ ბაზის წაშლა
    JetBrainsCleanupCode, //jb cleanupcode solutionFileName.sln -> JetBrain-ის უტილიტის გაშვება პროექტის სოლუშენის ფაილის მითითებით კოდის გასაწმენდად და მოსაწესრიგებლად
    JsonFromProjectDbProjectGetter, //არსებული პროდაქშენ ბაზის ასლიდან დაამზადებს json ფაილები თავიდან

    //ScaffoldSeederGitSync, //მხოლოდ გიტის სინქრონიზაცია სკაფოლდ სიდინგ პროექტებისათვის
    ScaffoldSeederCreator, //არსებული პროდაქშენ ბაზის ასლიდან დაამზადებს სკაფოლდ პროექტს და შექმნის ჯეისონ ფაილებს, რომლებიც შემდგომში შესაძლებელი იქნება გამოვიყენოთ სიდინგისათვის. ამ შემქმნელ ამოცანაში შედის კოდის შექმნა, როგორც სკაფოლდინგისათვის, ისე სიდინგისათვის
    SeedData, //scaffold-ით მიღებული ბაზიდან მიღებულ json-ებზე დაყრდნობით ინფორმაციის ჩაყრა დეველოპერ ბაზაში.
    PrepareProdCopyDatabase //პროდაქშენ ბაზის ასლის მომზადება სკაფოლდით დამუშავებისათვის

    ////-------------------------------------------------------------------------------
    ////ეს ინსტრუმენტები გამოიყენება თითოეული პროექტისათვისა და თითოეული სერვერისათვის
    //AppSettingsEncoder, //  EncodeParameters, //პარამეტრების დაშიფვრა

    //// EncodeParameters=>GenerateEncodedParametersFile=>UploadParametersToExchange
    //AppSettingsInstaller, //  InstallParameters, //დაშიფრული პარამეტრების განახლება

    ////(DownloadParameters=>StopProgramIfRunning=>UpdateParameters=>StartServiceIfRequired)
    //AppSettingsUpdater, //  UpdateParameters, //პარამეტრების დაშიფვრა და დაინსტალირებული პროგრამისთვის ამ დაშიფრული პარამეტრების გადაგზავნა-განახლება
    //DevBaseToServerCopier, //Dev ბაზის დაკოპირება სერვერზე.
    //ServiceInstallScriptCreator, //მოშორებულ სერვერზე გასაშვები საინსტალაციო სკრიპტის შექმნა
    //ServiceRemoveScriptCreator, //მოშორებულ სერვერზე გასაშვები საინსტალაციო სკრიპტის შექმნა
    //ServerBaseToProdCopyCopier, //სერვერიდან ბაზის დაკოპირება ProdCopy ბაზაში.

    ////EncodeParameters=>GenerateEncodedParametersFile=>UploadParametersToExchange=>
    ////(RunAppSettingsInstaller)(DownloadParameters=>StopProgramIfRunning=>UpdateParameters=>StartServiceIfRequired)
    //ProgPublisher, //  PublishProgram, //პროგრამის საინსტალაციო პაკეტის გამზადება

    ////CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters
    //ProgramInstaller, //  InstallProgram, //პროგრამის საინსტალაციო პაკეტის გამოყენებით პროგრამის დაინსტალირება-განახლება

    ////DownloadPackage=>DownloadParameters=>StopProgramIfRunning=>UpdateProgram=>UpdateParameters=>StartServiceIfRequired
    //ProgramUpdater, //  PublishAndInstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამზადება და პროგრამის დაინსტალირება-განახლება

    ////CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters=>
    ////(RunProgramInstaller)(DownloadPackage=>DownloadParameters=>StopProgramIfRunning=>UpdateProgram=>UpdateParameters=>StartServiceIfRequired)
    //ProgRemover, //  RemoveProgram, //პროგრამის წაშლა
    //ServiceStarter, //უკვე დაინსტალირებული სერვისის გაშვება სერვერის მხარეს.
    //ServiceStopper, //გაშვებული სერვისის გაჩერება სერვერის მხარეს.
    //VersionChecker //გაშვებული სერვისის ვერსიის დადგენა. (ეს იმუშავებს მხოლოდ იმ შემთხვევაში, თუ სერვისში გამოყენებული იქნება იქნება TestTools.Controllers.TestController
}