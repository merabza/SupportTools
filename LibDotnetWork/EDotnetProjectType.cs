namespace LibDotnetWork;

public enum EDotnetProjectType
{
    ClassLib,
    Console,
    Web,
    ReactEsProj
}

/*
 16-mar-2025

dotnet --version

9.0.201

dotnet new list

These templates matched your input:
   
   Template Name             Short Name                  Language    Tags
   ------------------------  --------------------------  ----------  -----------------------------------------------------------------------------
   .NET Aspire App Host      aspire-apphost              [C#]        Common/.NET Aspire/Cloud
   .NET Aspire Empty App     aspire                      [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service
   .NET Aspire Service D...  aspire-servicedefaults      [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service
   .NET Aspire Starter App   aspire-starter              [C#]        Common/.NET Aspire/Blazor/Web/Web API/API/Service/Cloud
   .NET Aspire Test Proj...  aspire-mstest               [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service/Test
   .NET Aspire Test Proj...  aspire-nunit                [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service/Test
   .NET Aspire Test Proj...  aspire-xunit                [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service/Test
   .NET MAUI App             maui                        [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Mobile
   .NET MAUI Blazor Hybr...  maui-blazor-web             [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Blazor/Blazor Hybrid/Mobile
   .NET MAUI Blazor Hybr...  maui-blazor                 [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Blazor/Blazor Hybrid/Mobile
   .NET MAUI Class Library   mauilib                     [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Mobile
   .NET MAUI ContentPage...  maui-page-csharp            [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI ContentPage...  maui-page-xaml              [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI ContentView...  maui-view-csharp            [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI ContentView...  maui-view-xaml              [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI Multi-Proje...  maui-multiproject           [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Mobile
   .NET MAUI ResourceDic...  maui-dict-xaml              [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Xaml/Code
   .NET MAUI Window (C#)     maui-window-csharp          [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI Window (XAML)   maui-window-xaml            [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   Android Activity          android-activity            [C#]        Android/Mobile
   Android Application       android                     [C#]        Android/Mobile
   Android Class Library     androidlib                  [C#]        Android/Mobile
   Android Java Library ...  android-bindinglib          [C#]        Android/Mobile
   Android Layout            android-layout              [C#]        Android/Mobile
   Android Wear Application  androidwear                 [C#]        Android/Mobile
   API Controller            apicontroller               [C#]        Web/ASP.NET
   ASP.NET Core Empty        web                         [C#],F#     Web/Empty
   ASP.NET Core gRPC Ser...  grpc                        [C#]        Web/gRPC/API/Service
   ASP.NET Core Web API      webapi                      [C#],F#     Web/WebAPI/Web API/API/Service
   ASP.NET Core Web API ...  webapiaot                   [C#]        Web/Web API/API/Service
   ASP.NET Core Web App ...  mvc                         [C#],F#     Web/MVC
   ASP.NET Core Web App ...  webapp,razor                [C#]        Web/MVC/Razor Pages
   ASP.NET Core with Ang...  angular                     [C#]        Web/MVC/SPA
   ASP.NET Core with Rea...  react                       [C#]        Web/MVC/SPA
   ASP.NET Core with Rea...  reactredux                  [C#]        Web/MVC/SPA
   Blazor Server App         blazorserver                [C#]        Web/Blazor
   Blazor Server App Empty   blazorserver-empty          [C#]        Web/Blazor/Empty
   Blazor Web App            blazor                      [C#]        Web/Blazor/WebAssembly
   Blazor WebAssembly Ap...  blazorwasm-empty            [C#]        Web/Blazor/WebAssembly/PWA/Empty
   Blazor WebAssembly St...  blazorwasm                  [C#]        Web/Blazor/WebAssembly/PWA
   Class Library             classlib                    [C#],F#,VB  Common/Library
   Console App               console                     [C#],F#,VB  Common/Console
   dotnet gitignore file     gitignore,.gitignore                    Config
   Dotnet local tool man...  tool-manifest                           Config
   EditorConfig file         editorconfig,.editorconfig              Config
   global.json file          globaljson,global.json                  Config
   iOS Application           ios                         [C#],F#,VB  iOS/Mobile
   iOS Binding Library       iosbinding                  [C#]        iOS/Mobile
   iOS Class Library         ioslib                      [C#],VB     iOS/Mobile
   iOS Controller            ios-controller              [C#]        iOS/Mobile
   iOS Storyboard            ios-storyboard              [C#]        iOS/Mobile
   iOS Tabbed Application    ios-tabbed                  [C#]        iOS/Mobile
   iOS View                  ios-view                    [C#]        iOS/Mobile
   iOS View Controller       ios-viewcontroller          [C#]        iOS/Mobile
   Mac Catalyst Application  maccatalyst                 [C#],VB     macOS/Mac Catalyst
   Mac Catalyst Binding ...  maccatalystbinding          [C#]        macOS/Mac Catalyst
   Mac Catalyst Class Li...  maccatalystlib              [C#],VB     macOS/Mac Catalyst
   Mac Catalyst Controller   maccatalyst-controller      [C#]        macOS/Mac Catalyst
   Mac Catalyst Storyboard   maccatalyst-storyboard      [C#]        macOS/Mac Catalyst
   Mac Catalyst View         maccatalyst-view            [C#]        macOS/Mac Catalyst
   Mac Catalyst View Con...  maccatalyst-viewcontroller  [C#]        macOS/Mac Catalyst
   MSBuild Directory.Bui...  buildprops                              MSBuild/props
   MSBuild Directory.Bui...  buildtargets                            MSBuild/props
   MSBuild Directory.Pac...  packagesprops                           MSBuild/packages/props/CPM
   MSTest Playwright Tes...  mstest-playwright           [C#]        Test/MSTest/Playwright/Desktop/Web
   MSTest Test Class         mstest-class                [C#],F#,VB  Test/MSTest
   MSTest Test Project       mstest                      [C#],F#,VB  Test/MSTest/Desktop/Web
   MVC Controller            mvccontroller               [C#]        Web/ASP.NET
   MVC ViewImports           viewimports                 [C#]        Web/ASP.NET
   MVC ViewStart             viewstart                   [C#]        Web/ASP.NET
   NuGet Config              nugetconfig,nuget.config                Config
   NUnit 3 Test Item         nunit-test                  [C#],F#,VB  Test/NUnit
   NUnit 3 Test Project      nunit                       [C#],F#,VB  Test/NUnit/Desktop/Web
   NUnit Playwright Test...  nunit-playwright            [C#]        Test/NUnit/Playwright/Desktop/Web
   Protocol Buffer File      proto                                   Web/gRPC
   Razor Class Library       razorclasslib               [C#]        Web/Razor/Library/Razor Class Library
   Razor Component           razorcomponent              [C#]        Web/ASP.NET
   Razor Page                page                        [C#]        Web/ASP.NET
   Razor View                view                        [C#]        Web/ASP.NET
   Solution File             sln,solution                            Solution
   Web Config                webconfig                               Config
   Windows Forms App         winforms                    [C#],VB     Common/WinForms
   Windows Forms Class L...  winformslib                 [C#],VB     Common/WinForms
   Windows Forms Control...  winformscontrollib          [C#],VB     Common/WinForms
   Worker Service            worker                      [C#],F#     Common/Worker/Web
   WPF Application           wpf                         [C#],VB     Common/WPF
   WPF Class Library         wpflib                      [C#],VB     Common/WPF
   WPF Custom Control Li...  wpfcustomcontrollib         [C#],VB     Common/WPF
   WPF User Control Library  wpfusercontrollib           [C#],VB     Common/WPF
   xUnit Test Project        xunit                       [C#],F#,VB  Test/xUnit/Desktop/Web
*/