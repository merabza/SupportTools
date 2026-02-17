//using System.Collections.Generic;
//using System.IO;
//using LibAppProjectCreator.JavaScriptCreators;
//using Microsoft.Extensions.Logging;
//using SystemToolsShared;

//// ReSharper disable ConvertToPrimaryConstructor

//namespace LibAppProjectCreator.React;

//public sealed class CreateReactClientApp
//{
//    private readonly string _destinationFolderPath;
//    private readonly ILogger _logger;
//    private readonly string _projectName;
//    private readonly Dictionary<string, string> _reactAppTemplates;
//    private readonly string _reactTemplateFolderName;

//    //private readonly IParametersManager? _parametersManager;
//    //private readonly string _tempFolderPath;
//    private readonly string _workFolder;

//    public CreateReactClientApp(ILogger logger, string destinationFolderPath, string projectName,
//        string reactTemplateName, string workFolder, Dictionary<string, string> reactAppTemplates)
//    {
//        _logger = logger;
//        _destinationFolderPath = destinationFolderPath;
//        _projectName = projectName;
//        _reactTemplateFolderName = reactTemplateName.ToLower();
//        _workFolder = workFolder;
//        _reactAppTemplates = reactAppTemplates;
//    }

//    //private bool RunNodeProcess(string command, string projectPath)
//    //{
//    //    var psiNpmRunDist = new ProcessStartInfo
//    //    {
//    //        FileName = "cmd",
//    //        RedirectStandardInput = true,
//    //        WorkingDirectory = projectPath
//    //    };
//    //    var pNpmRunDist = Process.Start(psiNpmRunDist);
//    //    if (pNpmRunDist == null)
//    //        return false;
//    //    pNpmRunDist.StandardInput.WriteLine($"{command} & exit");
//    //    pNpmRunDist.WaitForExit();

//    //    return true;
//    //}

//    public bool Run()
//    {
//        //რეაქტის პროექტებისათვის ინსტრუმენტების სამუშაო ფოლდერში გვაქვს სამი ფოლდერი შემდეგი შინაარსით:
//        //ReactAppModels - ფოლდერი სადაც ინახება npx create-react-app უტილიტით მოქაჩული ფაილები
//        //ReactAppModelsForDiff - ფოლდერი, სადაც ინახება ReactAppModels-დან დაკოპირებული ფაილები იმისათვის, რომ მომავალში დადგინდეს, თუ რომელი ფაილები შეიცვალა npx create-react-app უტილიტის გაშვებისას
//        //ReactAppModelsForUse - ფოლდერი, სადაც ინახება ის ვარიანტი, რომელიც გამოიყენება ეხლა, მისი განახლება შესაძლებელი უნდა იყოს მხოლოდ ხელით, ან თუ წაიშლება, მაშინ ეს კოდი შეეცდება მის განახლებას

//        //1. პირველ რიგში დავადგინოთ პროექტის შესაბამისი შაბლონის შესაბამისი  ფაილები არის თუ არა შენახული ReactAppModelsForUse ფოლდერში.
//        // 1.1. თუ არსებობს, ClientApp ფოლდერში დაკოპირდეს ReactAppModelsForUse ფოლდერში არსებული საჭირო ფაილები

//        //2. თუ ReactAppModelsForUse ფოლდერში შესაბამისი ფაილები ვერ მოიძებნა, მაშინ შესაბამისი ფაილები მოიძებნოს ReactAppModelsForDiff ფოლდერში
//        // 2.1. თუ ფაილები არსებობს, ჯერ დაკოპირდეს საჭირო ფაილები ReactAppModelsForDiff ფოლდერში 

//        //3. თუ ReactAppModelsForDiff ფოლდერში შესაბამისი ფაილები ვერ მოიძებნა, მაშინ გაეშვას ReCreateReactAppFilesByTemplateNameToolCommand,
//        //  რომელიც შექმნის ReactAppModelsForUse ფოლდერში შესაბამის პროექტს და შემდეგ უკვე იქიდან შესაძლებელი იქნება დაკოპირება.
//        //  თუ ამ ნაბიჯის მერეც არ იქნება შესაბამისი ფაილები ReactAppModelsForUse ფოლდერში, მაშინ ვჩერდებით შეცდომის გამოცხადებით

//        if (!_reactAppTemplates.TryGetValue(_reactTemplateFolderName, out var value))
//        {
//            StShared.WriteErrorLine($"{_reactTemplateFolderName} template does not exists in templates dictionary",
//                true,
//                _logger);
//            return false;
//        }

//        var forUseDir = GetWorkDirectory("ReactAppModelsForUse");
//        if (forUseDir is null) return false;

//        string[] excludes = [".git", "node_modules"];

//        //თუ ReactAppModelsForUse ფოლდერში შესაბამისი ფაილები ვერ მოიძებნა, მაშინ შესაბამისი ფაილები მოიძებნოს ReactAppModelsForDiff ფოლდერში
//        if (!forUseDir.Exists || (forUseDir.GetFiles().Length == 0 && forUseDir.GetDirectories().Length == 0))
//        {
//            var forDiffDir = GetWorkDirectory("ReactAppModelsForDiff");
//            if (forDiffDir is null) return false;
//            //თუ ReactAppModelsForDiff ფოლდერში შესაბამისი ფაილები ვერ მოიძებნა, მაშინ გაეშვას ReCreateReactAppFilesByTemplateNameToolCommand,
//            //  რომელიც შექმნის ReactAppModelsForUse ფოლდერში შესაბამის პროექტს და შემდეგ უკვე იქიდან შესაძლებელი იქნება დაკოპირება.
//            if (!forDiffDir.Exists || (forDiffDir.GetFiles().Length == 0 && forDiffDir.GetDirectories().Length == 0))
//            {
//                var reCreateReactAppFiles =
//                    new ReCreateReactAppFiles(_logger, _workFolder, _reactTemplateFolderName, value);
//                if (!reCreateReactAppFiles.Run())
//                    return false;
//            }

//            //თუ ამ ნაბიჯის მერეც არ იქნება შესაბამისი ფაილები ReactAppModelsForUse ფოლდერში, მაშინ ვჩერდებით შეცდომის გამოცხადებით
//            if (!forDiffDir.Exists || (forDiffDir.GetFiles().Length == 0 && forDiffDir.GetDirectories().Length == 0))
//            {
//                StShared.WriteErrorLine($"{forDiffDir.FullName} does not exists",
//                    true, _logger);
//                return false;
//            }

//            //todo დავაკოპიროთ ფაილები forDiffDir-დან forUseDir-ში
//            FileStat.CopyFilesAndFolders(forDiffDir.FullName, forUseDir.FullName, excludes, true, _logger);
//        }

//        if (!forUseDir.Exists || (forUseDir.GetFiles().Length == 0 && forUseDir.GetDirectories().Length == 0))
//        {
//            StShared.WriteErrorLine($"{forUseDir.FullName} does not exists",
//                true, _logger);
//            return false;
//        }

//        //todo დავაკოპიროთ ფაილები forUseDir-დან ClientApp-ში
//        FileStat.CopyFilesAndFolders(forUseDir.FullName, _destinationFolderPath, excludes, true, _logger);

//        //დროებით {_tempFolderPath} ფოლდერზე გადავიყვანოთ მიმდინარე ფოლდერი
//        //Directory.SetCurrentDirectory(_tempFolderPath);

//        //გავუშვათ რეაქტის პროექტის შექმნის ბრძანება
//        //npx create-react-app {projectName}
//        //if (!StShared.RunProcess(_logger, "C:\\Program Files\\nodejs\\npx", $"create-react-app {_projectName}"))
//        //{
//        //  StShared.WriteErrorLine("Error When creating react app");
//        //  return false;
//        //}
//        //if (!RunNodeProcess(
//        //        $"npx create-react-app {_projectName.ToLower()}{(string.IsNullOrWhiteSpace(_reactTemplateName) ? string.Empty : $" --template {_reactTemplateName}")}",
//        //        _tempFolderPath))
//        //{
//        //    StShared.WriteErrorLine("Error When creating react app", true, _logger);
//        //    return false;
//        //}

//        //დროებით ფოლდერში ახლადშექმნილი {projectName} ფოლდერიდან ფაილები უნდა გადავიტანოთ
//        //destinationFolderPath ფოლდერში საჭიროების მიხედვით
//        if (false)
//        {
//            //1. public ფოლდერი გადავიტანოთ მთლიანად
//            //var tempProjectClientFolderPath = Path.Combine(_tempFolderPath, _projectName);
//            //var publicPathFromTemp = Path.Combine(tempProjectClientFolderPath, "public");
//            var publicPath = Path.Combine(_destinationFolderPath, "public");
//            //Directory.Move(publicPathFromTemp, publicPath);

//            //2. {destinationFolderPath}/src ფოლდერში GIT უნდა მოვქაჩოთ carcass
//            //  git clone git@bitbucket.org:mzakalashvili/dncreactcarcass.git carcass
//            var srcPath = Path.Combine(_destinationFolderPath, "src");
//            if (!StShared.CreateFolder(srcPath, true))
//            {
//                StShared.WriteErrorLine($"Error When creating folder {srcPath}", true, _logger);
//                return false;
//            }

//            var srcCarcassPath = Path.Combine(srcPath, "carcass");
//            var carcassGitProjectName = "git@bitbucket.org:mzakalashvili/dncreactcarcass.git";
//            if (StShared.RunProcess(true, _logger, "git", $"clone {carcassGitProjectName} {srcCarcassPath}").IsSome)
//            {
//                StShared.WriteErrorLine(
//                    $"Error When cloning git {carcassGitProjectName} to folder {srcCarcassPath}",
//                    true, _logger);
//                return false;
//            }

//            //3. {destinationFolderPath}/src ფოლდერში შევქმნათ project ფოლდერი
//            var srcProjectPath = Path.Combine(srcPath, "project");
//            if (!StShared.CreateFolder(srcProjectPath, true))
//            {
//                StShared.WriteErrorLine($"Error When creating folder {srcProjectPath}", true, _logger);
//                return false;
//            }

//            //3a. {destinationFolderPath}/src/project ფოლდერში შევქმნათ store ფოლდერი
//            var srcProjectStorePath = Path.Combine(srcProjectPath, "store");
//            if (!StShared.CreateFolder(srcProjectStorePath, true))
//            {
//                StShared.WriteErrorLine($"Error When creating folder {srcProjectStorePath}", true, _logger);
//                return false;
//            }

//            //4. {tempFolderPath}/{projectName}/.gitignore -> {destinationFolderPath}/src/project/.gitignore
//            //File.Move(Path.Combine(tempProjectClientFolderPath, ".gitignore"), srcProjectPath, true);
//            const string gitignore = ".gitignore";
//            //File.Copy(Path.Combine(tempProjectClientFolderPath, gitignore),Path.Combine(srcProjectPath, gitignore));

//            //5. {tempFolderPath}/{projectName}/src/App.css -> {destinationFolderPath}/src/project/App.css
//            //var tempSrcPath = Path.Combine(tempProjectClientFolderPath, "src");
//            const string appCss = "App.css";
//            //File.Copy(Path.Combine(tempSrcPath, appCss), Path.Combine(srcProjectPath, appCss));

//            //6. {tempFolderPath}/{projectName}/src/App.test.js -> {destinationFolderPath}/src/project/App.test.js
//            const string appTestJs = "App.test.js";
//            //File.Copy(Path.Combine(tempSrcPath, appTestJs), Path.Combine(srcProjectPath, appTestJs));

//            //7. {tempFolderPath}/{projectName}/src/logo.svg -> {destinationFolderPath}/src/project/logo.svg
//            const string logoSvg = "logo.svg";
//            //File.Copy(Path.Combine(tempSrcPath, logoSvg), Path.Combine(srcProjectPath, logoSvg));

//            //8. {tempFolderPath}/{projectName}/src/index.css -> {destinationFolderPath}/src/index.css
//            const string indexCss = "index.css";
//            //File.Copy(Path.Combine(tempSrcPath, indexCss), Path.Combine(srcPath, indexCss));

//            //10. {tempFolderPath}/{projectName}/src/reportWebVitals.js -> {destinationFolderPath}/src/reportWebVitals.js
//            const string reportWebVitalsJs = "reportWebVitals.js";
//            //File.Copy(Path.Combine(tempSrcPath, reportWebVitalsJs), Path.Combine(srcPath, reportWebVitalsJs));

//            //11. {tempFolderPath}/{projectName}/src/setupTests.js -> {destinationFolderPath}/src/setupTests.js
//            const string setupTestsJs = "setupTests.js";
//            //File.Copy(Path.Combine(tempSrcPath, setupTestsJs), Path.Combine(srcPath, setupTestsJs));

//            //12. {tempFolderPath}/{projectName}/package.json -> {destinationFolderPath}/package.json
//            const string packageJson = "package.json";
//            //File.Copy(Path.Combine(tempProjectClientFolderPath, packageJson),Path.Combine(_destinationFolderPath, packageJson));

//            //13. {tempFolderPath}/{projectName}/README.md -> {destinationFolderPath}/README.md
//            const string readMeMd = "README.md";
//            //File.Copy(Path.Combine(tempProjectClientFolderPath, readMeMd), Path.Combine(_destinationFolderPath, readMeMd));

//            //14. D:\1WorkDotnetCore\GeoModel\grammar.ge.client\src\project\App.js-ის ანალოგიით შეიქმნას {destinationFolderPath}/src/project/App.js
//            AppJsCreator appJsCreator = new(srcProjectPath, "App.js");
//            appJsCreator.Create();

//            //15. D:\1WorkDotnetCore\GeoModel\grammar.ge.client\src\project\AuthAppRoutes.js-ის ანალოგიით შეიქმნას {destinationFolderPath}/src/project/AuthAppRoutes.js
//            AuthAppRoutesJsCreator authAppRoutesJsCreator = new(srcProjectPath, "AuthAppRoutes.js");
//            authAppRoutesJsCreator.Create();

//            //16. D:\1WorkDotnetCore\GeoModel\grammar.ge.client\src\project\Home.js-ის ანალოგიით შეიქმნას {destinationFolderPath}/src/project/Home.js
//            HomeJsCreator homeJsCreator = new(srcProjectPath, "Home.js");
//            homeJsCreator.Create();

//            //17. D:\1WorkDotnetCore\GeoModel\grammar.ge.client\src\project\ProjectProvider.js-ის ანალოგიით შეიქმნას {destinationFolderPath}/src/project/ProjectProvider.js
//            ProjectProviderJsCreator projectProviderJsCreator = new(srcProjectPath, _projectName, "ProjectProvider.js");
//            projectProviderJsCreator.Create();

//            //18. D:\1WorkDotnetCore\GeoModel\grammar.ge.client\src\project\TopNavRoutes.js-ის ანალოგიით შეიქმნას {destinationFolderPath}/src/project/TopNavRoutes.js
//            TopNavRoutesJsCreator topNavRoutesJsCreator = new(srcProjectPath, "TopNavRoutes.js");
//            topNavRoutesJsCreator.Create();

//            //19. D:\1WorkDotnetCore\GeoModel\grammar.ge.client\src\index.js-ის ანალოგიით შეიქმნას {destinationFolderPath}/src/index.js
//            IndexJsCreator indexJsCreator = new(srcPath, "index.js");
//            indexJsCreator.Create();

//            //20. D:\1WorkDotnetCore\GeoModel\grammar.ge.client\src\project\store\projectReducers.js-ის ანალოგიით შეიქმნას {destinationFolderPath}/src/project/store/projectReducers.js
//            ProjectReducersJsCreator projectReducersJsCreator = new(srcProjectStorePath, "projectReducers.js");
//            projectReducersJsCreator.Create();

//            //if (!RunNodeProcess("npm i", _destinationFolderPath))
//            //{
//            //    StShared.WriteErrorLine("Error when installing packages", true, _logger);
//            //    return false;
//            //}
//        }

//        return true;
//    }

//    private DirectoryInfo? GetWorkDirectory(string segmentName)
//    {
//        var segmentFolderFullName = Path.Combine(_workFolder, segmentName);

//        var checkedPath = FileStat.CreateFolderIfNotExists(segmentFolderFullName, true);
//        if (checkedPath is null)
//        {
//            StShared.WriteErrorLine($"does not exists and cannot create work folder {segmentFolderFullName}",
//                true, _logger);
//            return null;
//        }

//        var appFolderFullName = Path.Combine(segmentFolderFullName, _reactTemplateFolderName);

//        return new DirectoryInfo(appFolderFullName);
//    }
//}


