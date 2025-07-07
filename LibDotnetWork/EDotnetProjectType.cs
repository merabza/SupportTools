namespace LibDotnetWork;

public enum EDotnetProjectType
{
    ClassLib,
    Console,
    Web,
    ReactEsProj,
    webapp,//razor 
}

/*
 25-may-2025

dotnet --version

9.0.301

dotnet new list

These templates matched your input:
   
   Template Name                                 Short Name                          Language    Tags
   --------------------------------------------  ----------------------------------  ----------  -----------------------------------------------------------------------------
   .NET Aspire App Host                          aspire-apphost                      [C#]        Common/.NET Aspire/Cloud
   .NET Aspire Empty App                         aspire                              [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service
   .NET Aspire Service Defaults                  aspire-servicedefaults              [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service
   .NET Aspire Starter App                       aspire-starter                      [C#]        Common/.NET Aspire/Blazor/Web/Web API/API/Service/Cloud
   .NET Aspire Test Project (MSTest)             aspire-mstest                       [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service/Test
   .NET Aspire Test Project (NUnit)              aspire-nunit                        [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service/Test
   .NET Aspire Test Project (xUnit)              aspire-xunit                        [C#]        Common/.NET Aspire/Cloud/Web/Web API/API/Service/Test
   .NET MAUI App                                 maui                                [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Mobile
   .NET MAUI Blazor Hybrid and Web App           maui-blazor-web                     [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Blazor/Blazor Hybrid/Mobile
   .NET MAUI Blazor Hybrid App                   maui-blazor                         [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Blazor/Blazor Hybrid/Mobile
   .NET MAUI Class Library                       mauilib                             [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Tizen/Mobile
   .NET MAUI ContentPage (C#)                    maui-page-csharp                    [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI ContentPage (XAML)                  maui-page-xaml                      [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI ContentView (C#)                    maui-view-csharp                    [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI ContentView (XAML)                  maui-view-xaml                      [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI Multi-Project App                   maui-multiproject                   [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Mobile
   .NET MAUI ResourceDictionary (XAML)           maui-dict-xaml                      [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Xaml/Code
   .NET MAUI Window (C#)                         maui-window-csharp                  [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   .NET MAUI Window (XAML)                       maui-window-xaml                    [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/WinUI/Tizen/Xaml/Code
   Android Activity                              android-activity                    [C#]        Android/Mobile
   Android Application                           android                             [C#]        Android/Mobile
   Android Class Library                         androidlib                          [C#]        Android/Mobile
   Android Java Library Binding                  android-bindinglib                  [C#]        Android/Mobile
   Android Layout                                android-layout                      [C#]        Android/Mobile
   Android Wear Application                      androidwear                         [C#]        Android/Mobile
   API Controller                                apicontroller                       [C#]        Web/ASP.NET
   ASP.NET Core Empty                            web                                 [C#],F#     Web/Empty
   ASP.NET Core gRPC Service                     grpc                                [C#]        Web/gRPC/API/Service
   ASP.NET Core Web API                          webapi                              [C#],F#     Web/WebAPI/Web API/API/Service
   ASP.NET Core Web API (native AOT)             webapiaot                           [C#]        Web/Web API/API/Service
   ASP.NET Core Web App (Model-View-Controller)  mvc                                 [C#],F#     Web/MVC
   ASP.NET Core Web App (Razor Pages)            webapp,razor                        [C#]        Web/MVC/Razor Pages
   ASP.NET Core with Angular                     angular                             [C#]        Web/MVC/SPA
   ASP.NET Core with React.js                    react                               [C#]        Web/MVC/SPA
   ASP.NET Core with React.js and Redux          reactredux                          [C#]        Web/MVC/SPA
   Blazor Server App                             blazorserver                        [C#]        Web/Blazor
   Blazor Server App Empty                       blazorserver-empty                  [C#]        Web/Blazor/Empty
   Blazor Web App                                blazor                              [C#]        Web/Blazor/WebAssembly
   Blazor WebAssembly App Empty                  blazorwasm-empty                    [C#]        Web/Blazor/WebAssembly/PWA/Empty
   Blazor WebAssembly Standalone App             blazorwasm                          [C#]        Web/Blazor/WebAssembly/PWA
   Class Library                                 classlib                            [C#],F#,VB  Common/Library
   Console App                                   console                             [C#],F#,VB  Common/Console
   dotnet gitignore file                         gitignore,.gitignore                            Config
   Dotnet local tool manifest file               tool-manifest                                   Config
   EditorConfig file                             editorconfig,.editorconfig                      Config
   global.json file                              globaljson,global.json                          Config
   iOS Application                               ios                                 [C#],F#,VB  iOS/Mobile
   iOS Binding Library                           iosbinding                          [C#]        iOS/Mobile
   iOS Class Library                             ioslib                              [C#],VB     iOS/Mobile
   iOS Controller                                ios-controller                      [C#]        iOS/Mobile
   iOS Notification Content Extension            ios-notification-content-extension  [C#]        iOS/Mobile
   iOS Notification Service Extension            ios-notification-service-extension  [C#]        iOS/Mobile
   iOS Storyboard                                ios-storyboard                      [C#]        iOS/Mobile
   iOS Tabbed Application                        ios-tabbed                          [C#]        iOS/Mobile
   iOS View                                      ios-view                            [C#]        iOS/Mobile
   iOS View Controller                           ios-viewcontroller                  [C#]        iOS/Mobile
   Mac Catalyst Application                      maccatalyst                         [C#],VB     macOS/Mac Catalyst
   Mac Catalyst Binding Library                  maccatalystbinding                  [C#]        macOS/Mac Catalyst
   Mac Catalyst Class Library                    maccatalystlib                      [C#],VB     macOS/Mac Catalyst
   Mac Catalyst Controller                       maccatalyst-controller              [C#]        macOS/Mac Catalyst
   Mac Catalyst Storyboard                       maccatalyst-storyboard              [C#]        macOS/Mac Catalyst
   Mac Catalyst View                             maccatalyst-view                    [C#]        macOS/Mac Catalyst
   Mac Catalyst View Controller                  maccatalyst-viewcontroller          [C#]        macOS/Mac Catalyst
   MSBuild Directory.Build.props file            buildprops                                      MSBuild/props
   MSBuild Directory.Build.targets file          buildtargets                                    MSBuild/props
   MSBuild Directory.Packages.props file         packagesprops                                   MSBuild/packages/props/CPM
   MSTest Playwright Test Project                mstest-playwright                   [C#]        Test/MSTest/Playwright/Desktop/Web
   MSTest Test Class                             mstest-class                        [C#],F#,VB  Test/MSTest
   MSTest Test Project                           mstest                              [C#],F#,VB  Test/MSTest/Desktop/Web
   MVC Controller                                mvccontroller                       [C#]        Web/ASP.NET
   MVC ViewImports                               viewimports                         [C#]        Web/ASP.NET
   MVC ViewStart                                 viewstart                           [C#]        Web/ASP.NET
   NuGet Config                                  nugetconfig,nuget.config                        Config
   NUnit 3 Test Item                             nunit-test                          [C#],F#,VB  Test/NUnit
   NUnit 3 Test Project                          nunit                               [C#],F#,VB  Test/NUnit/Desktop/Web
   NUnit Playwright Test Project                 nunit-playwright                    [C#]        Test/NUnit/Playwright/Desktop/Web
   Protocol Buffer File                          proto                                           Web/gRPC
   Razor Class Library                           razorclasslib                       [C#]        Web/Razor/Library/Razor Class Library
   Razor Component                               razorcomponent                      [C#]        Web/ASP.NET
   Razor Page                                    page                                [C#]        Web/ASP.NET
   Razor View                                    view                                [C#]        Web/ASP.NET
   Solution File                                 sln,solution                                    Solution
   Web Config                                    webconfig                                       Config
   Windows Forms App                             winforms                            [C#],VB     Common/WinForms
   Windows Forms Class Library                   winformslib                         [C#],VB     Common/WinForms
   Windows Forms Control Library                 winformscontrollib                  [C#],VB     Common/WinForms
   Worker Service                                worker                              [C#],F#     Common/Worker/Web
   WPF Application                               wpf                                 [C#],VB     Common/WPF
   WPF Class Library                             wpflib                              [C#],VB     Common/WPF
   WPF Custom Control Library                    wpfcustomcontrollib                 [C#],VB     Common/WPF
   WPF User Control Library                      wpfusercontrollib                   [C#],VB     Common/WPF
   xUnit Test Project                            xunit                               [C#],F#,VB  Test/xUnit/Desktop/Web
*/