# დაკავშირებული პროექტები

SupportTools ახდენს მუშაობის კოორდინაციას ლოკალურ და მოშორებულ
მანქანებზე, ორი თანმდევი პროექტის გამოყენებით, რომლებიც საკუთარ
რეპოზიტორიებში ცხოვრობენ.

## WebAgent

რეპოზიტორია: [https://github.com/merabza/WebAgent](https://github.com/merabza/WebAgent)

ხანგრძლივად მოქმედი HTTP სერვისი, რომელიც ინსტალირდება თითოეულ
მართულ სერვერზე. იგი ხდის იგივე შესაძლებლობებს, რასაც SupportTools-ი
ლოკალურად — პროგრამის ინსტალაცია, სერვისის მართვა, ბაზის ოპერაცია —
ქსელის გავლით API-ით მისაწვდომი.

SupportTools ესაუბრება WebAgent-ს `WebAgentContracts` დის რეპოს
კონტრაქტებით (`WebAgentDatabasesApiContracts`,
`WebAgentProjectsApiContracts`).

SupportTools-ში მოშორებული deployment არის HTTP გამოძახებების
თანმიმდევრობა შენი დეველოპერ მანქანიდან WebAgent-ზე სამიზნე
სერვერზე, სადაც WebAgent ფაქტობრივ სამუშაოს ასრულებს სერვერის
ლოკალურ filesystem-სა და სერვისების მართვაში.

**რას აკეთებს WebAgent, რასაც SupportTools პირდაპირ ვერ აღწევს:**

* პროგრამის ინსტალაცია/წაშლა სერვერზე
* სერვისების გაშვება/გაჩერება
* სერვისის ვერსიის წაკითხვა (`VersionChecker` იძახებს WebAgent-ის
ვერსიის endpoint-ს)
* დაშიფრული `appsettings.json`-ის გამოყენება სერვისის install
ადგილისთვის
* ბაზის ოპერაციების შესრულება ისეთ ბაზებზე, რომელთაც დეველოპერი ვერ
აღწევს

\---

## WebAgentInstaller

რეპოზიტორია: [https://github.com/merabza/WebAgentInstaller](https://github.com/merabza/WebAgentInstaller)

პატარა bootstrapper, რომელიც გამოიყენება WebAgent-ის თვითონ
ინსტალაციისა და განახლებისთვის.

WebAgentInstaller-ი ერთხელ ინსტალირდება ახალ სერვერზე. იქიდან
WebAgentInstaller-ს შეუძლია ახალი ვერსიების WebAgent-ის წამოღება და
ინსტალაცია. ეს არის chicken-and-egg-ის გადაწყვეტა: WebAgent ვერ
ანახლებს თავის თავს გაშვებისას, ამიტომ WebAgentInstaller აკეთებს ამას.

მას შემდეგ რაც WebAgentInstaller სერვერზეა, შენ მას აღარასოდეს ეხები
ხელით — SupportTools მართავს მას იმავე კონტრაქტებით, როგორც
WebAgent-ს.

\---

## როდის რომელი

|სცენარი|ვინ აკეთებს სამუშაოს|
|-|-|
|ოპერაციები დეველოპერის საკუთარ მანქანაზე|SupportTools (ლოკალური გზა)|
|ოპერაციები მოშორებულ სერვერზე|SupportTools → WebAgent (მოშორებული გზა)|
|ახალი სერვერის პირველი setup|WebAgentInstaller-ის, შემდეგ WebAgent-ის ხელით ინსტალაცია|
|WebAgent-ის განახლება სერვერზე|SupportTools → WebAgentInstaller → ინსტალირებს ახალ WebAgent-ს|

`IsLocal` ფლაგი `ServerDataModel`-ზე ირჩევს გზას. იხ.
[კონფიგურაცია](configuration.md).

\---

## ვერსიის გადაბმა

სამივე ხელსაწყო იზიარებს კონტრაქტებს `WebAgentContracts` რეპოს
მეშვეობით. ვერსიის შეუსაბამობა SupportTools-ის მხარესა და WebAgent-ის
მხარეს შორის ჩნდება როგორც ჩუმი ჩავარდნა health check-ების დროს —
იხ. [განთავსება](use-cases/deployment.md) "WebAgent-ის ვერსიის
შეუსაბამობა"-ს pitfall-ისთვის.

ყველაზე უსაფრთხო პრაქტიკა: განაახლე WebAgent სერვერზე **სანამ** deploy
გააკეთებ პროექტისთვის, რომლის SupportTools tooling განახლდა.

\---

## დაკავშირებული

* [განთავსება](use-cases/deployment.md) — როგორ გამოიყენება მოშორებული
გზა
* [არქიტექტურა](architecture.md) — `LibSupportToolsServerWork` არის
client-ის მხარე

