using SystemToolsShared;

namespace LibAppProjectCreator.AppCreators;

public class ReactEsProjectCreator
{
    private readonly string _projectFullPath;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReactEsProjectCreator(string projectFullPath, bool useConsole)
    {
        _projectFullPath = projectFullPath;
        _useConsole = useConsole;
    }

    public void Create()
    {
        //შეიქმნას ფოლდერი სადაც უნდა ჩაიწეროს რეაქტის ფრონტ პროექტი
        StShared.CreateFolder(_projectFullPath, _useConsole);

        //შეიქმნას .esproj ფაილი. ნიმუში:
        /*
         *<Project Sdk="Microsoft.VisualStudio.JavaScript.Sdk/0.5.45-alpha">
             <PropertyGroup>
               <StartupCommand>set BROWSER=none&amp;&amp;npm start</StartupCommand>
             <JavaScriptTestRoot>src\</JavaScriptTestRoot>
               <JavaScriptTestFramework>Jest</JavaScriptTestFramework>
               <!-- Command to run on project build -->
               <BuildCommand></BuildCommand>
               <!-- Command to create an optimized build of the project that's ready for publishing -->
               <ProductionBuildCommand>npm run build</ProductionBuildCommand>
               <!-- Folder where production build objects will be placed -->
               <BuildOutputFolder>$(MSBuildProjectDirectory)\build</BuildOutputFolder>
             </PropertyGroup>
           </Project>
         */

        //Microsoft.VisualStudio.JavaScript.Sdk-ს ბოლო ვერსიის დასადგენად უნდა მოვქაჩოთ გვერდი 
        //https://www.nuget.org/packages/Microsoft.VisualStudio.JavaScript.SDK
        //გავაანალიზოთ მოქაჩული html დოკუმენტი.
        //ამოვიღოთ ყველა ასეთი ლინკი
        //<a href="/packages/Microsoft.VisualStudio.JavaScript.SDK/1.0.1477582" title="1.0.1477582">
        //პირველი, ან ყველაზე მაღალ ნომრიანი გამოვიყენოთ .esproj ფაილის xml-ის შესაქმნელად
        //დანარჩენი ინფორმაცია ამ xml-ში გადავა ისე, როგორც ნიმუშშია.

        //შეიქმნას რეაქტის პროექტი შესაბამისი სკრიპტის გამოყენებით 
        //npx create-react-app my-app --template typescript

        //შევქმნათ src ფოლდერი და მისი შიგთავსი

    }
}