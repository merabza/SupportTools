# დაწყება

როგორ ვაკლონირო ყველა საჭირო რეპოზიტორია, ავაწყო და გავუშვა SupportTools.

## წინაპირობები

* .NET 10 SDK
* Git (SSH წვდომით საწყის რეპოებთან)
* არჩევითი: JetBrains ReSharper Command Line Tools — საჭიროა მხოლოდ
`JetBrainsCleanupCode` ბრძანებისთვის

\---

## ყველა საჭირო რეპოზიტორიის კლონირება

SupportTools-ი დამოკიდებულია ათ დის რეპოზიტორიაზე. შექმენი მთავარი
ფოლდერი და დააკლონირე ისინი ერთმანეთის გვერდით.

```bash
mkdir SupportTools
cd SupportTools
git clone git@github.com:merabza/AppCliTools.git              AppCliTools
git clone git@github.com:merabza/BackendCarcass.git           BackendCarcass
git clone git@github.com:merabza/BackendCarcassShared.git     BackendCarcassShared
git clone git@github.com:merabza/ConnectionTools.git          ConnectionTools
git clone git@github.com:merabza/DatabaseTools.git            DatabaseTools
git clone git@github.com:merabza/ParametersManagement.git     ParametersManagement
git clone git@github.com:merabza/SupportTools.git             SupportTools
git clone git@github.com:merabza/SupportToolsServerShared.git SupportToolsServerShared
git clone git@github.com:merabza/SystemTools.git              SystemTools
git clone git@github.com:merabza/ToolsManagement.git          ToolsManagement
git clone git@github.com:merabza/WebAgentContracts.git        WebAgentContracts
cd ..
```

ამის შემდეგ უნდა გქონდეს ასეთი სტრუქტურა:

```
SupportTools/
  ├── AppCliTools/
  ├── BackendCarcass/
  ├── BackendCarcassShared/
  ├── ConnectionTools/
  ├── DatabaseTools/
  ├── ParametersManagement/
  ├── SupportTools/                 ← ეს რეპო
  │   └── SupportTools.slnx
  ├── SupportToolsServerShared/
  ├── SystemTools/
  ├── ToolsManagement/
  └── WebAgentContracts/
```

Solution-ი თითოეულ დის რეპოს ეხება ფარდობითი ბილიკით
(`../AppCliTools/...` და სხვ.), ამიტომ ეს განლაგება სავალდებულოა.

\---

## აწყობა

`SupportTools/` რეპოდან:

```bash
dotnet build SupportTools.slnx
```

Build მკაცრად ცდის `TreatWarningsAsErrors`, `AnalysisMode=All`, და
`EnforceCodeStyleInBuild` (იხ. [განვითარება](development.md)). ნებისმიერი
warning წყვეტს build-ს.

\---

## გაშვება

```bash
dotnet run --project SupportTools/SupportTools.csproj
```

ან გაუშვი executable პირდაპირ build-ის შემდეგ:

```bash
./SupportTools/bin/Debug/net10.0/SupportTools.exe
```

აპლიკაცია იწყებს ინტერაქტიულ CLI მენიუს. მთავარი მენიუ აწყობილია
`SupportToolsMenuBuilder`-ში და ჩამოთვლილია
`SupportTools/Menu/MenuData.cs`-ში (`MainMenuCommandFactoryStrategyNames`).

\---

## პირველი გაშვება

პირველი გაშვებისას, SupportTools ქმნის პარამეტრების JSON ფაილს შენი
user profile-ში და აჩვენებს მთავარ მენიუს ცარიელი პროექტებით. აქედან:

1. **ძირითადი პარამეტრების რედაქტირება** —
`SupportToolsParametersEditorListCliMenuCommandFactoryStrategy`. დააყენე
გლობალური ბილიკები (work folder, security folder, code-generate test
folder).
2. **პროექტის ჯგუფის შექმნა** — გახსენი `Project Groups List`, დაამატე
ჯგუფი.
3. **პროექტის დამატება** — ან:
* `Create New Project` (შაბლონზე დაფუძნებული, იხ.
[პროექტის შექმნა](use-cases/project-creation.md)), ან
* `Import Project` თუ გაქვს ექსპორტირებული JSON სხვა მანქანიდან.
4. **Git რეპოების კონფიგურაცია** პროექტისთვის — იხ.
[Git ოპერაციები](use-cases/git-operations.md).
5. **სინქი** — `Sync All Projects All Gits V2` დააკლონირებს ყველაფერს.

State ფაილის სრული სტრუქტურისთვის იხ.
[კონფიგურაცია](configuration.md).

\---

## ინსტალაციის შემოწმება

წარმატებული ინსტალაცია გადის ამ შემოწმებებს:

* `dotnet build SupportTools.slnx` მთავრდება შეცდომების და warning-ების
გარეშე
* Executable-ის გაშვება აჩვენებს მთავარ მენიუს `Project Groups List`-ით,
`Sync All Projects All Gits V2`-ით და სხვ.
* `Recent Commands List`-ის არჩევა აჩვენებს ცარიელ სიას (ჯერ ისტორია არ
არის) ახალ ინსტალაციაზე

\---

## დაკავშირებული

* [არქიტექტურა](architecture.md) — რას აკეთებენ მოდულები
* [კონფიგურაცია](configuration.md) — პარამეტრების ფაილის სტრუქტურა
* [განვითარება](development.md) — code style, analyzer-ები, ბრძანებების
დამატება

