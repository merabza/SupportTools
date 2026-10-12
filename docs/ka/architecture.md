# არქიტექტურა

SupportTools არის მოდულური .NET 10 კონსოლის აპლიკაცია. Solution
დაყოფილია ერთ entry-point executable-ად, ერთ application-bootstrap
პროექტად, ერთ data პროექტად, და თერთმეტ `Lib\*` ბიბლიოთეკად, რომელიც
დაჯგუფებულია დომენების მიხედვით.

Solution ასევე იცავს ცხრა გარე რეპოზიტორიას როგორც დის checkout-ებს
(იხ. [დაწყება](getting-started.md) clone-ის სიისთვის). მითითებები
შენახულია `.csproj`-ის დონეზე ფარდობითი ბილიკებით; build ელის რომ
ყველა დის რეპო `SupportTools/`-ის გვერდით ცხოვრობს.

\---

## მოდულების ცნობარი

|მოდული|დანიშნულება|ძირითადი დამოკიდებულებები|
|-|-|-|
|`SupportToolsData`|დომენის მოდელები, enum-ები (`EProjectTools`), JSON state-ის ფორმა|— (შიდა dep-ების გარეშე)|
|`LibGitData`|Git რეპოზიტორიის მეტამონაცემების მონაცემთა სტრუქტურები|SystemTools.SystemToolsShared|
|`LibTools`|საერთო ჰელპერები და caching|LibGitWork, SupportToolsData|
|`LibDotnetWork`|`dotnet` CLI ორკესტრაცია (build, restore, ef)|SystemTools.SystemToolsShared|
|`LibGitWork`|`git` ბრძანებების შესრულება და რეპოს მანიპულაცია|SystemTools.SystemToolsShared|
|`LibNpmWork`|npm-ის ორკესტრაცია frontend ამოცანებისთვის|SystemTools.SystemToolsShared|
|`LibDatabaseWork`|ბაზის სასიცოცხლო ციკლი: create, drop, migrate, correct|LibDotnetWork, SupportToolsData|
|`LibCodeGenerator`|კოდის გენერაცია (API რაუტები, პროექტის არტიფაქტები)|LibGitWork, SupportToolsData|
|`LibAppInstallWork`|Install/uninstall pipeline-ები (ლოკალური და მოშორებული)|LibDotnetWork, SupportToolsData|
|`LibSupportToolsServerWork`|ცენტრალური რეესტრი: საერთო კოლექციების სინქრონიზაცია SupportToolsServer-თან (იხ. [რეესტრის ფენა](#რეესტრის-ფენა)); SupportToolsServer-ის რედაქტორები (`Support Tools Server Editor`); ყველაფერი სერვერის კლიენტით, `SupportToolsServerApiClient`|LibGitWork, SupportToolsData|
|`LibAppProjectCreator`|პროექტის scaffolding შაბლონებიდან|LibAppInstallWork, LibGitWork, LibNpmWork|
|`LibScaffoldSeeder`|Scaffold + seed კოდი არსებული ბაზებიდან|LibAppProjectCreator, LibGitWork|
|`SupportTools.Application`|DI host, კონფიგურაცია, პარამეტრების მართვა|გარე: AppCliTools.CliTools, ParametersManagement.\*|
|`SupportTools`|კონსოლის entry point, მენიუს wiring, CLI ბრძანებების factory-ები|ყველა Lib\* პროექტი|

\---

## დამოკიდებულებების შრეები

```
შრე 5    Entry
         ┌──────────────────────────────────────────────────────────┐
         │ SupportTools (Exe)                                       │
         │ SupportTools.Application                                 │
         └──────────────────────────────────────────────────────────┘
                                  │
შრე 4    მაღალი დონის workflow-ები ▼
         ┌──────────────────────────────────────────────────────────┐
         │ LibAppProjectCreator      LibScaffoldSeeder              │
         └──────────────────────────────────────────────────────────┘
                                  │
შრე 3    დომენის ლოგიკა            ▼
         ┌──────────────────────────────────────────────────────────┐
         │ LibDatabaseWork  LibCodeGenerator                        │
         │ LibAppInstallWork  LibSupportToolsServerWork             │
         └──────────────────────────────────────────────────────────┘
                                  │
შრე 2    პროცესის ორკესტრაცია     ▼
         ┌──────────────────────────────────────────────────────────┐
         │ LibDotnetWork  LibGitWork  LibNpmWork                    │
         └──────────────────────────────────────────────────────────┘
                                  │
შრე 1    საფუძველი                ▼
         ┌──────────────────────────────────────────────────────────┐
         │ SupportToolsData  LibGitData  LibTools                   │
         └──────────────────────────────────────────────────────────┘
```

წაიკითხეთ ზემოდან: `SupportTools/Menu/`-ში executable ბრძანება იყენებს
ორკესტრატორს `LibAppInstallWork`-ში ან `LibScaffoldSeeder`-ში,
რომელიც გადასცემს `LibDotnetWork`/`LibGitWork`/`LibNpmWork`-ს
პროცესის სამუშაოს, რომელიც იყენებს `LibGitData`/`SupportToolsData`-ს
ტიპიზებული state-ისთვის.

\---

## გარე რეპოზიტორიები, რომლებზეც solution-ი დაყრდნობილია

ეს არის დის Git checkout-ები, რომლებიც მითითებულია ფარდობითი ბილიკებით
`SupportTools.slnx`-ში. ისინი იმართება როგორც განცალკევებული
რეპოზიტორიები:

|რეპო|გამოყენება|
|-|-|
|`AppCliTools`|CLI მენიუ, პარამეტრების რედაქტირება, მონაცემთა შეტანა, seed-კოდის შექმნა|
|`BackendCarcass`|გაზიარებული backend framework პროექტები|
|`BackendCarcassShared`|Carcass კონტრაქტები|
|`ConnectionTools`|FTP/connection-pool utility-ები|
|`DatabaseTools`|SQL Server, SQLite, OleDb ადაპტერები|
|`ParametersManagement`|API client, file, database პარამეტრების მართვა|
|`SupportToolsServerShared`|SupportToolsServer-თან გაზიარებული კონტრაქტები|
|`SystemTools`|System აბსტრაქციები, background task-ები, ბაზის ხელსაწყოების გაზიარებული ტიპები|
|`ToolsManagement`|API client-ები, კომპრესია, ბაზები, file manager-ები, installer|
|`WebAgentContracts`|WebAgent API-ის კონტრაქტები (databases, projects)|

\---

## Entry point

`SupportTools/Program.cs` აკეთებს სტანდარტულ CLI bootstrap-ს:

1. `ArgumentsAnalyzer` კითხულობს CLI args-ს;
`ParametersService<SupportToolsParameters>` ტვირთავს პარამეტრების ფაილს
2. `ServiceCollection.AddServices(...)` (`SupportTools.Application`-ში)
აყენებს DI-ს
3. `CliAppLoopParameters.Create<Program>(...)` ააწყობს მენიუს ციკლს
4. `CliAppLoop.Run()` მუშაობს მანამდე, სანამ მომხმარებელი არ გავა

მენიუს ბრძანებები რეგისტრირდება როგორც factory strategy-ები
`SupportTools/Menu/`-ში. მთავარი მენიუ აწყობილია
`SupportToolsMenuBuilder`-ში `MenuData.MainMenuCommandFactoryStrategyNames`-დან.

\---

## რეესტრის ფენა

პარამეტრების ფაილის საერთო კოლექციები (პროექტები, სერვერები, git
რეპოები, შაბლონები, API client-ები, …) SupportToolsServer-ზეც ინახება —
ეს ცენტრალური რეესტრია, რომ ყველა კომპიუტერი ერთსა და იმავე მონაცემს
ხედავდეს (იხ.
[კონფიგურაცია](configuration.md#ცენტრალური-რეესტრი-supporttoolsserver)).
სინქრონიზაცია `LibSupportToolsServerWork/Registry/`-შია:

|ნაწილი|როლი|
|-|-|
|`Paths/`|`PathMapper`: კანონიკური გზები (Windows-ის, როგორც მთავარ კომპიუტერზე) ↔ ამ კომპიუტერის გზები, `PathMappings`-ით|
|`Mappers/`|თითო სუფთა mapper-ი კოლექციაზე: ლოკალური მოდელი ↔ სერვერის კონტრაქტი (`Sts…DataModel`), კანონიკური გზებით და ჰეშისთვის ნორმალიზებული ფორმით|
|`Adapters/`|თითო ადაპტერი კოლექციაზე (სულ 18): ლოკალურ და სერვერის ჩანაწერებს კითხულობს და ორივე მხარეს წერს. `RegistrySyncAdapterFactory` მათ აწყობს; `RegistryCollections` მათ სახელებს და რიგს ადგენს (მითითებული კოლექციები ჯერ, `Projects` ბოლოს)|
|`Sync/`|ძრავა: `RegistrySyncPlanner` ყოველ ჩანაწერს ადარებს აქ, სერვერზე და ბოლო სინქრონიზაციისას (`RegistrySyncState`: სერვერის `Version` და ჰეში თითო ჩანაწერზე) და გეგმავს pull-ს, push-ს ან კონფლიქტს; `RegistrySyncEngine` არჩეულ ნაწილს ასრულებს და პარამეტრებს ერთხელ ინახავს|
|`MachineLocalFields`|ველები, რომლებიც არასოდეს სინქრონიზდება|

ძრავას UI არ აქვს. მას `SupportTools` პროექტი იყენებს:

* `Menu/SyncRegistry/` — `Sync Registry With SupportToolsServer...` და
მისი ერთკოლექციიანი მალსახმობები `Gits`-ისა და შაბლონების სიების
მენიუებში;
* `AutoSync/` — ჩამოტანა გაშვებისას და გაგზავნა ყოველი შენახვის შემდეგ:
DI-ის `IParametersManager` დეკორატორია, `AutoSyncParametersManager`;
* `Menu/ProjectGroupsList/ProjectsList/CloneProjectToThisComputer/` —
`remote` პროექტი ამ კომპიუტერზე ჩამოაქვს.

`LibSupportToolsServerWork`-შივეა `Support Tools Server Editor`, რომელიც
სერვერის git რეპოებსა და შაბლონებს პირდაპირ არედაქტირებს (ყოველ
ჩაწერამდე სერვერის `Version`-ს ამოწმებს), და `GitProjects/`, რომელიც იმ
`GitProjects`-ს გარდაქმნის, რომლებსაც სერვერი თავისი კლონებიდან ითვლის.

\---

## State

ყველა მუდმივი state — პროექტები, ჯგუფები, server info-ები, allow-tool
სიები, Git URL-ები — ცხოვრობს ერთ JSON პარამეტრების ფაილში. ბილიკი
მოწოდებულია `ArgumentsAnalyzer`-ით (`--use` პარამეტრი), ფაილს კი
ტვირთავს `ParametersService` (`--use`-ის გარეშე მიმდინარე და გამშვები
ფაილის ფოლდერში ეძებს).
იხ. [კონფიგურაცია](configuration.md). ფაილის საერთო ნაწილი
SupportToolsServer-ზეც ინახება (იხ. [რეესტრის ფენა](#რეესტრის-ფენა));
ფაილი სამუშაო ასლად რჩება.

\---

## დაკავშირებული

* [დაწყება](getting-started.md) — clone სია, build, პირველი გაშვება
* [განვითარება](development.md) — code style, ანალიზი, მენიუს გაფართოება
* [დაკავშირებული პროექტები](related-projects.md) — WebAgent /
WebAgentInstaller

