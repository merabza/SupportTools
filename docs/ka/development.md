# განვითარება

როგორ ვიმუშაო SupportTools-ზე: build settings, code style, და
ფუნქციონალის დამატების შაბლონები.

## Build settings

ყველა პროექტი იღებს `Directory.Build.props`-დან:

```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>disable</ImplicitUsings>
<Nullable>enable</Nullable>

<AnalysisLevel>latest</AnalysisLevel>
<AnalysisMode>All</AnalysisMode>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

შედეგები:

* Implicit `using`-ები გათიშულია — თითო ფაილი თვითონ აცხადებს თავის
using-ებს
* Nullable reference types ჩართულია
* ყველა ანალიზატორის warning არის build-ის შეცდომა
* ყველა code-style დარღვევა (`.editorconfig`-დან) არის build-ის შეცდომა

`SonarAnalyzer.CSharp` მუშაობს მთელ solution-ზე. **პერ-პროექტი
override-ი არ არის** — იგივე წესები ვრცელდება ყველგან.

\---

## პაკეტების მართვა

NuGet ვერსიები ცენტრალურად დაფიქსირებულია `Directory.Packages.props`-ში
(`ManagePackageVersionsCentrally = true`). ცალკეული `.csproj` ფაილები
ჩამოთვლის `<PackageReference Include="X" />` ვერსიის გარეშე, და ვერსია
მოდის ცენტრალური props ფაილიდან.

ახალი პაკეტის დამატებისას:

1. დაამატე `<PackageVersion Include="Foo" Version="x.y.z" />`
`Directory.Packages.props`-ში
2. დაამატე `<PackageReference Include="Foo" />` მომხმარებელ
`.csproj`-ში

\---

## Code style

`.editorconfig` არის ჭეშმარიტების წყარო. Build მკაცრად მოითხოვს
დაცვას. ძირითადი პროექტ-მასშტაბიანი კონვენციები, რომლებიც წყაროდან
ჩანს:

* `// ReSharper disable once <rule>` გამოიყენება ReSharper-ის
ლოკალური ჩასაჩუმებლად, ვიდრე გლობალურად გათიშვა
* Primary constructor-ები არიდებენ (კოდს აქვს
`// ReSharper disable once ConvertToPrimaryConstructor` ნიშნები)
* Service კლასები `sealed` სადაც პრაქტიკულია
* `using` დირექტივები სორტირებულია (`dotnet format` ნაბიჯი
`JetBrainsCleanupCode`-ში ალაგებს მათ)

\---

## ახალი მენიუს ბრძანების დამატება

ბრძანება არის factory strategy-ის და command-კლასის წყვილი. Walk-through:

1. **აარჩიე ადგილი** — `SupportTools/Menu/`-ში, ფოლდერში, რომელიც
შეესაბამება მენიუს დონეს (`ProjectGroupsList`, `ProjectsList` და სხვ.)
2. **შექმენი `\*CliMenuCommandFactoryStrategy`** — არეგისტრირებს
ბრძანებას menu builder-ში
3. **შექმენი `\*CliMenuCommand`** — იმპლემენტირებს ფაქტობრივ ქმედებას,
ჩვეულებრივად გადასცემს შესაბამის `Lib\*` ბიბლიოთეკაში არსებულ
`\*ToolAction` კლასს
4. **დაარეგისტრირე strategy-ი** — დაამატე `nameof(MyNewCommandFactoryStrategy)`
შესაბამის სიაში `SupportTools/Menu/MenuData.cs`-ში
* `MainMenuCommandFactoryStrategyNames` ზედა დონისთვის
* `ProjectGroupSubMenuCommandFactoryStrategyNames` ჯგუფის დონისთვის
* `ProjectSubMenuCommandFactoryStrategyNames` პროექტის დონისთვის

DI კონტეინერი ავტომატურად აღმოაჩენს factory strategy-ებს
`SupportTools.Application`-ის service registration-ით.

\---

## ახალი პერ-პროექტი tool action-ის დამატება

პერ-პროექტი ხელსაწყოები (ჩამოთვლილი `EProjectTools` /
`EProjectServerTools`-ში) იყენებენ განსხვავებულ შაბლონს:

1. **დაამატე enum მნიშვნელობა** `SupportToolsData/EProjectTools.cs`-ში
ან `EProjectServerTools.cs`-ში, ქართული კომენტარით, რომელიც აჯამებს
დანიშნულებას (შეესაბამება არსებულ სტილს)
2. **იმპლემენტირე `\*ToolAction`** შესაბამის `Lib\*` ბიბლიოთეკაში
3. **დააკავშირე action-ი** enum-თან `ToolCommandFactory.cs`-ში
(`SupportTools/ToolCommandFactory.cs`)
4. **გამოაჩინე ხელსაწყო** `SelectProjectAllowTools`-ის მეშვეობით, რომ
მომხმარებლებს შეეძლოთ მისი ჩართვა პროექტში

\---

## კონფიგურაციის მოდელში ველის დამატება

1. **დაამატე თვისება** შესაბამის მოდელში
`SupportToolsData/Models/`-ში
2. **დაამატე `FieldEditor`** `SupportTools/FieldEditors/`-ში in-app
რედაქტირებისთვის
3. **დააკავშირე editor-ი** შესაბამის `\*ParametersEditor`-ში
`SupportTools/ParametersEditors/`-ში ან `Cruders/`-ში
4. **გაუშვი** — არსებული JSON ფაილები იტვირთება, რადგან
Newtonsoft.Json იგნორირებს გამოტოვებულ თვისებებს და იყენებს default-ებს
ახლებისთვის

\---

## ტესტირება

unit ტესტები `SupportTools.Tests` პროექტშია (xUnit + Moq), რომელიც
`SupportTools.slnx`-ის ნაწილია:

```bash
dotnet test SupportTools.slnx
```

დაფარვა ჯერ მწირია — ტესტები მხოლოდ რამდენიმე მენიუს ბრძანებას აქვს —
ამიტომ დადასტურება ძირითადად ისევ ხელითაა: build + run + მენიუს გავლა.

ტესტების დამატებისას:

* სატესტო პროექტზე `Directory.Build.props`-იდან იგივე მკაცრი build
ვრცელდება (ანალიზატორის ყველა გაფრთხილება შეცდომაა). ტესტების
სახელები `Method_State_Expected` ფორმით დასაშვებია — `CA1707`
`.editorconfig`-ში გამორთულია.
* `SupportTools.csproj` თავის `internal` წევრებს `SupportTools.Tests`-ს
უხსნის (`InternalsVisibleTo`).
* `Inputer`-ის მოთხოვნები კონსოლს პირდაპირ კითხულობს (`Console.ReadKey`),
რაც სატესტო ჰოსტში არ მუშაობს. ინტერაქტიული მენიუს ბრძანების
გასატესტად მიეცი მას internal კონსტრუქტორი, რომელიც შეყვანის
ფუნქციებს იღებს — იხ. `SaveGitIgnoreAsNewTemplateCliMenuCommand`.
* `Console.SetOut` მთელ პროცესზე ვრცელდება, xUnit-ში კი სატესტო კლასები
პარალელურად მუშაობს. სატესტო კლასს, რომელიც `Console.SetOut`-ს იყენებს,
მიუთითე `[Collection(ConsoleCaptureCollection.Name)]` — ეს კოლექცია
პარალელიზაციის გარეშე მუშაობს, ამიტომ ტესტები ერთმანეთს ხელს არ უშლის.

`stryker-report/` ფოლდერი არსებობს, მაგრამ ცარიელია და არცერთ
pipeline-ში არ არის ჩართული (იხ. [კოდის ხარისხი](use-cases/code-quality.md)).

\---

## Debugging

* აპლიკაცია იტვირთება ნელა პირველ რიგზე, რადგან კითხულობს სრულ
პარამეტრების JSON-ს. სწრაფი იტერაციისთვის, მიუთითე `--use`-ით
უფრო პატარა scratch JSON ფაილი ერთი პროექტით.
* `Serilog` არის ლოგერი; შეამოწმე ბილიკი `LogFolder`-ის ქვეშ შენი
პარამეტრების ფაილში.
* `StShared.WriteException` (`SystemTools.SystemToolsShared`-დან)
თანმიმდევრულად აფორმატებს გამონაკლისებს — გამოიყენე ის ახალ კოდში,
ვიდრე ნედლი `Console.WriteLine`.

\---

## დაკავშირებული

* [არქიტექტურა](architecture.md) — მოდულების layering, რომ სწორ
შრეში დაამატო
* [კონფიგურაცია](configuration.md) — მოდელის ველის ცნობარი
* [კოდის ხარისხი](use-cases/code-quality.md) — analyzer-ები და cleanup

