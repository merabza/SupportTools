# კონფიგურაცია

ყველა მუდმივი state ცხოვრობს ერთ JSON ფაილში, რომელსაც მართავს
`ParametersManager` (გარე `ParametersManagement` ბიბლიოთეკიდან). ეს
გვერდი აღწერს ფაილის სტრუქტურას, რომ მოძებნო ან დაარედაქტირო settings-ი.

## ფაილის მდებარეობა

პარამეტრების ფაილის ბილიკი მოწოდებულია `--use` არგუმენტით, ფაილს
ტვირთავს `ParametersService`. `--use`-ის გარეშე `SupportTools.json`
ორ ადგილას ეძებება, ამ თანმიმდევრობით:

* მიმდინარე დირექტორია
* გამშვები ფაილის ფოლდერი

ავტომატური ძებნა მხოლოდ უკვე არსებულ ფაილს იყენებს — შექმნას არ
სთავაზობს. `--use`-ით მითითებული არარსებული ან დაზიანებული ფაილის
შექმნა კი შემოთავაზდება. ფაილის სახელი იყენებს date mask-ს `SupportToolsParameters.ParametersFileDateMask`-დან
და გაფართოებას `ParametersFileExtension`-დან.

\---

## ზედა დონე: `SupportToolsParameters`

განსაზღვრულია `SupportToolsData/Models/SupportToolsParameters.cs`-ში.
თვისებების ჯგუფები:

|ჯგუფი|თვისებები|გამოყენება|
|-|-|-|
|ბილიკები|`LogFolder`, `WorkFolder`, `TempFolder`, `SecurityFolder`, `PublisherWorkFolder`, `CodeGenerateTestFolder`, `ScaffoldSeedersWorkFolder`, `FolderForGitignoreFiles`, `FolderForEditorConfigFiles`, `GitExecutablePath`|სად კითხულობს/წერს ხელსაწყო დისკზე; `FolderForGitignoreFiles` — `.gitignore` შაბლონების ფაილების ფოლდერი; `FolderForEditorConfigFiles` — `.editorconfig` შაბლონების ფაილების ფოლდერი; `GitExecutablePath` — git-ის გამშვები ფაილის სრული გზა (თუ ცარიელია, გამოიყენება უბრალოდ `git` `PATH`-იდან; რედაქტორი ავტომატურად ადგენს Windows-ზე `Get-Command git`-ით / Linux-ზე `which git`-ით)|
|გაცვლა|`FileStorageNameForExchange`, `SmartSchemaNameForExchange`, `UploadTempExtension`|საჭიროა AppSettings encode/install-ისთვის (იხ. [განთავსება](use-cases/deployment.md))|
|ბრძანებების ისტორია|`RecentCommandsFileName`, `RecentCommandsCount`|მენიუს ისტორია|
|არქივები|`ProgramArchiveDateMask`, `ProgramArchiveExtension`, `ParametersFileDateMask`, `ParametersFileExtension`|პაკეტირების კონვენციები|
|კოლექციები|`Projects`, `Servers`, `Gits`, `GitProjects`|რეგისტრირებული პროექტები, server ჩანაწერები, git რეპოები, პერ-პროექტი git mapping-ები|
|შაბლონები|`Templates`, `ReactAppTemplates`, `NpmPackages`, `Environments`, `RunTimes`, `GitIgnorePatterns`, `EditorConfigPatterns`|პროექტის creator-ის შენატანი; `GitIgnorePatterns` / `EditorConfigPatterns` — ასევე შაბლონები, რომლების მიხედვით მოწმდება `.gitignore` / `.editorconfig` ფაილები (იხ. [შაბლონები](#gitignore-და-editorconfig-შაბლონები))|
|ინფრასტრუქტურა|`DotnetTools`, `ApiClients`, `Archivers`, `DatabaseServerConnections`, `FileStorages`, `SmartSchemas`|გადასაბუნებელი გაზიარებული რესურსები|

\---

## `ProjectModel`

თითოეული რეგისტრირებული პროექტის ბირთვი. მდებარეობს
`SupportToolsData/Models/ProjectModel.cs`-ში. ველების ჯგუფები:

|ჯგუფი|ველები|
|-|-|
|იდენტობა|`ProjectGroupName`, `ProjectName`, `ProjectDescription`, `ProjectFolderName`, `SolutionFileName`, `EditorConfigPatternName` (იხ. [შაბლონები](#gitignore-და-editorconfig-შაბლონები))|
|პროექტის ტიპი|`ProjectType` (Standard/IsService/IsPackage), `UseAlternativeWebAgent`|
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
* `GitIgnorePatternName` — მითითება კონფიგურირებულ `.gitignore` შაბლონზე
(ერთ-ერთი `GitIgnorePatterns` სიიდან; მისი ფაილია
`{FolderForGitignoreFiles}\{GitIgnorePatternName}.gitignore`)

**`GitProjectDataModel`** — git რეპოს და მასში არსებული `.csproj`
ფაილების mapping:

* `GitName` — უკავშირდება `GitDataModel`-ს
* `ProjectRelativePath`, `ProjectFileName` — `.csproj`-ის მდებარეობა
რეპოში
* `DependsOnProjectNames` — build დამოკიდებულების მინიშნებები
თანმიმდევრობისთვის

\---

## `.gitignore` და `.editorconfig` შაბლონები

შაბლონების ორივე სახეობა ჩვეულებრივი ფაილია შაბლონების ფოლდერში,
პარამეტრებში სახელების სიით. ორივე სია რედაქტირდება
`Support Tools Parameters Editor`-ში (`Git Ignore Patterns` /
`Editor Config Patterns`). თითოეული სიის მენიუში არის `Check ... Files`
(სტატუსში ნაჩვენებია, რამდენი ფაილი განსხვავდება თავისი შაბლონისგან ან
არ არსებობს) და `Update ... Files` (ასეთ ფაილებს შაბლონის შიგთავსით
გადააწერს).

||`.gitignore`|`.editorconfig`|
|-|-|-|
|ველი|`GitDataModel.GitIgnorePatternName` — git რეპოს ველი (აუცილებელი)|`ProjectModel.EditorConfigPatternName` — პროექტის ველი (არააუცილებელი; თუ ცარიელია, პროექტი არ მოწმდება)|
|ფაილი|`.gitignore` რეპოს ფოლდერში, თითოეულ პროექტში, რომელშიც რეპო შედის|`.editorconfig` პროექტის `SolutionFileName` ფაილის ფოლდერში; solution ფაილის გარეშე პროექტი არ მოწმდება|
|შაბლონის ფაილი|`{FolderForGitignoreFiles}\{სახელი}.gitignore`|`{FolderForEditorConfigFiles}\{სახელი}.editorconfig`|
|ახალი შაბლონი|პროექტი → Git მენიუ → რეპო → `Save .gitignore as New Template`|პროექტი → `Save .editorconfig as New Template`|

ახალი შაბლონის შენახვისას ბრძანება ფაილს შაბლონების ფოლდერში აკოპირებს
(თუ ფოლდერი არ არსებობს, იქმნება) და სახელს სიაში ამატებს; თვითონ
რეპოს / პროექტის შაბლონის სახელი არ იცვლება.

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

