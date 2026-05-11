# კონფიგურაცია

ყველა მუდმივი state ცხოვრობს ერთ JSON ფაილში, რომელსაც მართავს
`ParametersManager` (გარე `ParametersManagement` ბიბლიოთეკიდან). ეს
გვერდი აღწერს ფაილის სტრუქტურას, რომ მოძებნო ან დაარედაქტირო settings-ი.

## ფაილის მდებარეობა

პარამეტრების ფაილის ბილიკი მოწოდებულია `ArgumentsParser`-ით და default-ად
ემთხვევა მომხმარებლის OS profile-ის დირექტორიას:

* Windows: `%USERPROFILE%\\<...>.json`
* Unix-მსგავსი: `\~/<...>.json`

შეგიძლია გადასცე ალტერნატიული ბილიკი როგორც CLI არგუმენტი. ფაილის
სახელი იყენებს date mask-ს `SupportToolsParameters.ParametersFileDateMask`-დან
და გაფართოებას `ParametersFileExtension`-დან.

\---

## ზედა დონე: `SupportToolsParameters`

განსაზღვრულია `SupportToolsData/Models/SupportToolsParameters.cs`-ში.
თვისებების ჯგუფები:

|ჯგუფი|თვისებები|გამოყენება|
|-|-|-|
|ბილიკები|`LogFolder`, `WorkFolder`, `TempFolder`, `SecurityFolder`, `PublisherWorkFolder`, `CodeGenerateTestFolder`, `ScaffoldSeedersWorkFolder`|სად კითხულობს/წერს ხელსაწყო დისკზე|
|გაცვლა|`FileStorageNameForExchange`, `SmartSchemaNameForExchange`, `UploadTempExtension`|საჭიროა AppSettings encode/install-ისთვის (იხ. [განთავსება](use-cases/deployment.md))|
|ბრძანებების ისტორია|`RecentCommandsFileName`, `RecentCommandsCount`|მენიუს ისტორია|
|არქივები|`ProgramArchiveDateMask`, `ProgramArchiveExtension`, `ParametersFileDateMask`, `ParametersFileExtension`|პაკეტირების კონვენციები|
|კოლექციები|`Projects`, `Servers`, `Gits`, `GitProjects`|რეგისტრირებული პროექტები, server ჩანაწერები, git რეპოები, პერ-პროექტი git mapping-ები|
|შაბლონები|`Templates`, `ReactAppTemplates`, `NpmPackages`, `Environments`, `RunTimes`, `GitIgnoreModelFilePaths`|პროექტის creator-ის შენატანი|
|ინფრასტრუქტურა|`DotnetTools`, `ApiClients`, `Archivers`, `DatabaseServerConnections`, `FileStorages`, `SmartSchemas`|გადასაბუნებელი გაზიარებული რესურსები|

\---

## `ProjectModel`

თითოეული რეგისტრირებული პროექტის ბირთვი. მდებარეობს
`SupportToolsData/Models/ProjectModel.cs`-ში. ველების ჯგუფები:

|ჯგუფი|ველები|
|-|-|
|იდენტობა|`ProjectGroupName`, `ProjectName`, `ProjectDescription`, `ProjectFolderName`, `SolutionFileName`|
|სერვისის ფლაგი|`IsService`, `UseAlternativeWebAgent`|
|ქვე-პროექტების სახელები|`MainProjectName`, `ApiContractsProjectName`, `SpaProjectName`, `DbContextProjectName`, `DbContextName`, `ProjectShortPrefix`|
|მიგრაცია და seeding|`MigrationStartupProjectFilePath`, `MigrationProjectFilePath`, `SeedProjectFilePath`, `SeedProjectParametersFilePath`, `MigrationSqlFilesFolder`|
|Scaffold|`ScaffoldSeederProjectName`, `NewDataSeedingClassLibProjectName`, `ExcludesRulesParametersFilePath`|
|შიფვრა|`AppSetEnKeysJsonFileName`, `KeyGuidPart`|
|ბაზა|`DevDatabaseParameters`, `ProdCopyDatabaseParameters`|
|კოლექციები|`Endpoints`, `RouteClasses`, `ServerInfos`, `GitProjectNames`, `ScaffoldSeederGitProjectNames`, `FrontNpmPackageNames`, `RedundantFileNames`|

\---

## `ServerInfoModel` და `ServerDataModel`

პროექტის `ServerInfos` სია არის environment-ის მიხედვით. თითოეული
ჩანაწერი ფლობს deployment routing-ს კონკრეტული environment-name +
server-name კომბინაციისთვის.

**`ServerInfoModel`** — პერ-პროექტი, პერ-environment:

* `EnvironmentName`, `ServerName` (composite key)
* `ServerSidePort`, `ApiVersionId` — საჭიროა ერთად health check-ისთვის
* `WebAgentNameForCheck` — რომელი API client გამოვიყენო ვერსიის
შესამოწმებლად
* `AppSettingsJsonSourceFileName`, `AppSettingsEncodedJsonFileName` —
appSettings input და output ბილიკები
* `ServiceUserName` — სამიზნე სერვერის სერვისის ანგარიში
* `AllowToolsList` — რომელი `EProjectServerTools` ქმედებებია ჩართული
* `CurrentDatabaseParameters`, `NewDatabaseParameters` — ბაზის
swap-ის თვალყურის დევნება მიგრაციების დროს

**`ServerDataModel`** — გლობალური, გადასაბუნებელი პროექტებში:

* `IsLocal` — მართავს ტრანსპორტს (`true` = ლოკალური install folder,
`false` = WebAgent HTTP-ით)
* `WebAgentName`, `WebAgentInstallerName` — API client იდენტიფიკატორები
`ApiClients` ლექსიკონში
* `FilesUserName`, `FilesUsersGroupName` — OS-დონის ანგარიში
* `Runtime` — სამიზნე .NET runtime
* `ServerSideDownloadFolder`, `ServerSideDeployFolder` — მოშორებული
ბილიკები

\---

## `GitDataModel` და `GitProjectDataModel`

**`GitDataModel`** — დასაკლონირებელი რეპოზიტორია:

* `GitProjectAddress` — clone URL (SSH ან HTTPS)
* `GitProjectFolderName` — ლოკალური ფოლდერის სახელი clone-ისთვის
* `GitIgnorePathName` — მითითება კონფიგურირებულ `.gitignore` შაბლონზე

**`GitProjectDataModel`** — git რეპოს და მასში არსებული `.csproj`
ფაილების mapping:

* `GitName` — უკავშირდება `GitDataModel`-ს
* `ProjectRelativePath`, `ProjectFileName` — `.csproj`-ის მდებარეობა
რეპოში
* `DependsOnProjectNames` — build დამოკიდებულების მინიშნებები
თანმიმდევრობისთვის

\---

## შიფვრა და secrets

ორი პროექტის ველი მართავს AppSettings შიფვრას:

|ველი|როლი|
|-|-|
|`KeyGuidPart`|პერ-პროექტი GUID. სიმეტრიული გასაღების ერთი ნახევარი.|
|`AppSetEnKeysJsonFileName`|JSON ფაილი, რომელშიც ჩამოთვლილია `appsettings.json`-ის რომელი ბილიკები იშიფრება.|

ფაქტობრივი გასაღები არის `SHA256(KeyGuidPart + ServerName.Capitalize())`
— ორივე ნახევარი უნდა ემთხვეოდეს encoder-ის (დეველოპერის მანქანა) და
decoder-ის (სამიზნე სერვისი) მხარეებს შორის.

სხვა პოტენციურად სენსიტიური კოლექციები `SupportToolsParameters`-ში:

* `DatabaseServerConnections` — ბაზის connection string-ები
სერთიფიკატებით
* `ApiClients` — endpoint URL-ები და API key-ები (გამოყენებული
WebAgent-ისა და SupportToolsServer-ის მიერ)
* `FileStorages` — მოშორებული storage-ის სერთიფიკატები

ყველა ეს შენახულია ღია ტექსტად პარამეტრების JSON-ში. დაიცავი
პარამეტრების ფაილი file-system-ის უფლებებით.

\---

## რედაქტირება

ველების უმეტესობა რედაქტირებადია in-app მენიუს მეშვეობით:

* `Support Tools Parameters Edit` — ზედა დონის ველები
* `Support Tools Server Edit` — `ApiClients`, `Servers`,
`DatabaseServerConnections`
* პერ-პროექტი მენიუები — პროექტ-სპეციფიური ველები, server info-ები,
git სიები

JSON ფაილის პირდაპირი რედაქტირება მუშაობს ერთჯერადი fix-ებისთვის,
მაგრამ მენიუს editor-ები ვალიდირებენ, ფაილი კი არა.

\---

## დაკავშირებული

* [არქიტექტურა](architecture.md) — რომელი ბიბლიოთეკა ფლობს რომელ მოდელს
* [განთავსება](use-cases/deployment.md) — როგორ გამოიყენება შიფვრა
* [განვითარება](development.md) — ველის დამატება მოდელში

