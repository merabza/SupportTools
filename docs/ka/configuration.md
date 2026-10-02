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

## შენახვა და backup-ები

მენიუში გაკეთებული ყოველი ცვლილება მაშინვე ინახება. `ParametersManager`
ახალ შიგთავსს ჯერ იმავე ფოლდერის დროებით ფაილში წერს და მერე მით
ანაცვლებს პარამეტრების ფაილს, ამიტომ შეწყვეტილი შენახვა ფაილს ვერ
დააზიანებს. თუ შიგთავსი არ შეცვლილა, ფაილი თავიდან არ იწერება.

ჩანაცვლებამდე ფაილის წინა ვერსია მის გვერდით კოპირდება
`<ფაილის სახელი>.yyyyMMdd-HHmmss-fff.bak` სახელით, მაგალითად
`SupportTools.json.20261001-213015-123.bak`. ინახება ბოლო 10 ასლი,
უფრო ძველები იშლება. იშლება მხოლოდ ზუსტად ასე დასახელებული ფაილები:
ხელით გაკეთებული, სხვა სახელის ასლები რჩება. ეს ეხება
`ParametersManager`-ით შენახულ ყველა ფაილს, მაგალითად ბოლო ბრძანებების
ფაილსაც.

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
არ არსებობს), `Update ... Files` (ასეთ ფაილებს შაბლონის შიგთავსით
გადააწერს) და `Sync ... files...`.

`Sync ... files...` სიას ადარებს იმ შაბლონებს, რომლებსაც SupportToolsServer
თავის ბაზაში ინახავს. ჩანაწერები სახელით ემთხვევა, რეგისტრის
გაუთვალისწინებლად. სიის ყველა შაბლონის ფაილი უნდა არსებობდეს, თორემ
ბრძანება ჩერდება. ჯერ ის განსხვავებებს აჩვენებს: შაბლონებს, რომელთა
შიგთავსი განსხვავდება, და შაბლონებს, რომლებიც მხოლოდ კლიენტზე ან მხოლოდ
სერვერზეა. შემდეგ ოთხ მიმართულებას სთავაზობს, თითოეულს იმის მოკლე
აღწერით, რას შეცვლის:

* `Merge Up` — ახალ და შეცვლილ შაბლონებს სერვერზე ტვირთავს;
* `Sync Up` — იგივე, და ამასთან სერვერზე შლის ჩანაწერებს, რომლებიც
სიაში არ არის;
* `Merge Down` — სერვერის ახალ და შეცვლილ შაბლონებს შაბლონების ფოლდერში
წერს, ახალ სახელებს კი სიაში ამატებს;
* `Sync Down` — იგივე, და ამასთან სერვერზე არარსებულ შაბლონებს სიიდან
შლის, მათ ფაილებთან ერთად.

`Merge` არაფერს შლის. გამოყენებული შაბლონი არ იშლება: `.gitignore`
შაბლონი, რომელსაც იმავე მხარეს git რეპო იყენებს (კლიენტზე `Gits`-ში,
სერვერზე სერვერის git რეპოებში), ან `.editorconfig` შაბლონი, რომელსაც
პროექტი იყენებს (მხოლოდ კლიენტზე; სერვერზე მათ არაფერი იყენებს).

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

* `Support Tools Parameters Editor` — ზედა დონის ველები, მათ შორის
`ApiClients`, `Servers` და `DatabaseServerConnections` კოლექციები
* `Support Tools Server Editor` — SupportToolsServer-ზე შენახული
ჩანაწერები: `GitIgnore File Types`, `EditorConfig File Types` და
`Gits from SupportToolsServer`
* პერ-პროექტი მენიუები — პროექტ-სპეციფიური ველები, server info-ები,
git სიები

`Gits from SupportToolsServer`-ში git-ის წაშლა ან გადარქმევა ლოკალურ
`Gits`-ის ჩანაწერსაც ცვლის, როცა სერვერი ცვლილებას მიიღებს. წაშლილი git
ლოკალურად რჩება, სანამ მას რომელიმე პროექტი იყენებს. გადარქმევისას ახალ
სახელზე გადადის პროექტების მიმართვებიც (`GitProjectNames`,
`ScaffoldSeederGitProjectNames`) და `GitProjects`-ის ჩანაწერებიც, თუ ახალი
სახელი ლოკალურად უკვე დაკავებული არ არის.

JSON ფაილის პირდაპირი რედაქტირება მუშაობს ერთჯერადი fix-ებისთვის,
მაგრამ მენიუს editor-ები ვალიდირებენ, ფაილი კი არა.

\---

## დაკავშირებული

* [არქიტექტურა](architecture.md) — რომელი ბიბლიოთეკა ფლობს რომელ მოდელს
* [განთავსება](use-cases/deployment.md) — როგორ გამოიყენება შიფვრა
* [განვითარება](development.md) — ველის დამატება მოდელში

