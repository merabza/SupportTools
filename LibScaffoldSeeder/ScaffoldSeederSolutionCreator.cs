using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppCliTools.DbContextAnalyzer.CodeCreators;
using LibAppProjectCreator;
using LibAppProjectCreator.AppCreators;
using LibAppProjectCreator.CodeCreators;
using LibGitData.Domain;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder;

public sealed class ScaffoldSeederSolutionCreator : AppCreatorBase
{
    private readonly ScaffoldSeederCreatorParameters _par;
    private readonly ScaffoldSeederCreatorData _scaffoldSeederCreatorData;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ScaffoldSeederSolutionCreator(ILogger logger, IHttpClientFactory httpClientFactory,
        ScaffoldSeederCreatorParameters scaffoldSeederCreatorParameters, string projectName, int indentSize,
        ScaffoldSeederCreatorData scaffoldSeederAppCreatorData) : base(logger, httpClientFactory, projectName,
        indentSize, scaffoldSeederCreatorParameters.GitProjects, scaffoldSeederCreatorParameters.GitRepos,
        scaffoldSeederAppCreatorData.AppCreatorBaseData.WorkPath,
        scaffoldSeederAppCreatorData.AppCreatorBaseData.SecurityPath,
        scaffoldSeederAppCreatorData.AppCreatorBaseData.SolutionPath)
    {
        _par = scaffoldSeederCreatorParameters;
        _scaffoldSeederCreatorData = scaffoldSeederAppCreatorData;
    }

    protected override void PrepareProjectsData()
    {
        //სკაფოლდინგის ბიბლიოთეკა
        AddProject(_scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject);

        //ბაზაში ინფორმაციის ჩამყრელი ბიბლიოთეკა
        AddProject(_scaffoldSeederCreatorData.DataSeedingClassLibProject);

        //სიდერის კოდის შემქმნელი აპლიკაცია
        AddProject(_scaffoldSeederCreatorData.CreateProjectSeederCodeProject);

        //ბაზიდან ცხრილების შიგთავსის json-ის სახით წამოღებისათვის საჭირო პროექტი
        AddProject(_scaffoldSeederCreatorData.GetJsonFromProjectDbProject);

        //მიგრაციის პროექტი ბიბლიოთეკა
        AddProject(_scaffoldSeederCreatorData.DbMigrationProject);

        //ინფორმაციის ბაზაში ჩაყრის პროცესის გამშვები პროექტი
        AddProject(_scaffoldSeederCreatorData.SeedDbProject);

        //პროექტი, რომელიც იქმნება მხოლოდ იმისათვის, რომ შესაძლებელი გახდეს dotnet EF ბრძანებების შესრულება შეცდომების გარეშე
        //მთავარი ამ პროექტში არის IHost-ის რეალიზაცია
        AddProject(_scaffoldSeederCreatorData.FakeHostWebApiProject);
    }

    //პროექტის ტიპისათვის დამახასიათებელი დამატებითი პარამეტრების გამოანგარიშება
    protected override bool PrepareSpecific()
    {
        if (string.IsNullOrWhiteSpace(_par.DbContextProjectName))
        {
            StShared.WriteErrorLine($"{nameof(ProjectModel.DbContextProjectName).Humanize()} not specified", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_par.NewDataSeedingClassLibProjectName))
        {
            StShared.WriteErrorLine(
                $"{nameof(ProjectModel.NewDataSeedingClassLibProjectName).Humanize()} not specified", true);
            return false;
        }

        GitProjectDataDomain mainDatabaseProject = GitProjects.GetGitProjectByKey(_par.DbContextProjectName);
        GitProjectDataDomain newDataSeedingClassLibProject =
            GitProjects.GetGitProjectByKey(_par.NewDataSeedingClassLibProjectName);

        //სკაფოლდინგის ბიბლიოთეკა
        AddPackage(_scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject,
            NuGetPackages.MicrosoftEntityFrameworkCoreSqlServer); //, "5"

        //ბაზაში ინფორმაციის ჩამყრელი ბიბლიოთეკა
        AddPackage(_scaffoldSeederCreatorData.DataSeedingClassLibProject,
            NuGetPackages.MicrosoftExtensionsLoggingAbstractions);
        AddReference(_scaffoldSeederCreatorData.DataSeedingClassLibProject, GitProjects.BackendCarcassDb);
        AddReference(_scaffoldSeederCreatorData.DataSeedingClassLibProject, GitProjects.BackendCarcassDataSeeding);
        AddReference(_scaffoldSeederCreatorData.DataSeedingClassLibProject, GitProjects.BackendCarcassIdentity);
        AddReference(_scaffoldSeederCreatorData.DataSeedingClassLibProject, GitProjects.BackendCarcassRepositories);
        AddReference(_scaffoldSeederCreatorData.DataSeedingClassLibProject, mainDatabaseProject);

        //სიდერის კოდის შემქმნელი აპლიკაცია
        AddPackage(_scaffoldSeederCreatorData.CreateProjectSeederCodeProject,
            NuGetPackages.MicrosoftEntityFrameworkCoreDesign); //, "5"
        AddReference(_scaffoldSeederCreatorData.CreateProjectSeederCodeProject, GitProjects.AppCliToolsCliParameters);
        AddReference(_scaffoldSeederCreatorData.CreateProjectSeederCodeProject,
            GitProjects.AppCliToolsDbContextAnalyzer);
        AddReference(_scaffoldSeederCreatorData.CreateProjectSeederCodeProject, mainDatabaseProject);
        AddReference(_scaffoldSeederCreatorData.CreateProjectSeederCodeProject,
            _scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject);

        //ბაზიდან ცხრილების შიგთავსის json-ის სახით წამოღებისათვის საჭირო პროექტი
        AddPackage(_scaffoldSeederCreatorData.GetJsonFromProjectDbProject,
            NuGetPackages.MicrosoftExtensionsLoggingAbstractions); //, "5"
        AddReference(_scaffoldSeederCreatorData.GetJsonFromProjectDbProject, GitProjects.AppCliToolsCliParameters);
        AddReference(_scaffoldSeederCreatorData.GetJsonFromProjectDbProject, GitProjects.AppCliToolsDbContextAnalyzer);
        AddReference(_scaffoldSeederCreatorData.GetJsonFromProjectDbProject,
            _scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject);

        //მიგრაციის პროექტი ბიბლიოთეკა
        AddReference(_scaffoldSeederCreatorData.DbMigrationProject, mainDatabaseProject);

        //ინფორმაციის ბაზაში ჩაყრის პროცესის გამშვები პროექტი
        AddPackage(_scaffoldSeederCreatorData.SeedDbProject, NuGetPackages.MicrosoftEntityFrameworkCoreDesign); //, "5"
        AddPackage(_scaffoldSeederCreatorData.SeedDbProject, NuGetPackages.MicrosoftExtensionsConfigurationUserSecrets);

        AddReference(_scaffoldSeederCreatorData.SeedDbProject, GitProjects.AppCliToolsCliParameters);
        AddReference(_scaffoldSeederCreatorData.SeedDbProject, GitProjects.BackendCarcassDataSeeding);
        AddReference(_scaffoldSeederCreatorData.SeedDbProject, GitProjects.AppCliToolsDbContextAnalyzer);
        AddReference(_scaffoldSeederCreatorData.SeedDbProject, GitProjects.ParametersManagementLibDatabaseParameters);
        AddReference(_scaffoldSeederCreatorData.SeedDbProject, mainDatabaseProject);
        //AddReference(_scaffoldSeederCreatorData.SeedDbProject, _scaffoldSeederCreatorData.DataSeedingClassLibProject);
        AddReference(_scaffoldSeederCreatorData.SeedDbProject, _scaffoldSeederCreatorData.DbMigrationProject);
        AddReference(_scaffoldSeederCreatorData.SeedDbProject, newDataSeedingClassLibProject);

        //პროექტი, რომელიც იქმნება მხოლოდ იმისათვის, რომ შესაძლებელი გახდეს dotnet EF ბრძანებების შესრულება შეცდომების გარეშე
        //მთავარი ამ პროექტში არის IHost-ის რეალიზაცია
        AddPackage(_scaffoldSeederCreatorData.FakeHostWebApiProject, NuGetPackages.MicrosoftEntityFrameworkCoreDesign);

        AddReference(_scaffoldSeederCreatorData.FakeHostWebApiProject, _scaffoldSeederCreatorData.DbMigrationProject);

        return true;
    }

    protected override async Task<bool> MakeAdditionalFiles(CancellationToken cancellationToken = default)
    {
        //შეიქმნას Program.cs. პროგრამის გამშვები კლასი
        Console.WriteLine("Creating Empty Console Program.cs...");
        var emptyConsoleProgramClassCreator = new EmptyConsoleProgramClassCreator(Logger,
            _scaffoldSeederCreatorData.CreateProjectSeederCodeProject.ProjectFullPath, "Program.cs");
        emptyConsoleProgramClassCreator.CreateFileStructure();

        string fakeHosProjectPath = _scaffoldSeederCreatorData.FakeHostWebApiProject.ProjectFullPath;
        //შეიქმნას Program.cs. პროგრამის გამშვები კლასი
        Console.WriteLine("Creating Fake Host Program.cs...");
        var fakeHostProgramClassCreator =
            new FakeHostConsoleProgramClassCreator(Logger, fakeHosProjectPath, "Program.cs");
        fakeHostProgramClassCreator.CreateFileStructure();

        //var scaffoldSeederFolderName = $"{_par.ScaffoldSeederProjectName}ScaffoldSeeder";
        //var scaffoldSeederSecurityFolderName = $"{scaffoldSeederFolderName}.sec";
        //var projectWorkFolderPath = Path.Combine(_par.ScaffoldSeedersWorkFolder, _par.ScaffoldSeederProjectName);
        //var solutionSecurityFolderPath = Path.Combine(projectWorkFolderPath, scaffoldSeederSecurityFolderName);
        //const string jsonExt = ".json";

        string parametersFileName = Path.Combine(_par.ProjectSecurityFolderPath,
            $"{_scaffoldSeederCreatorData.FakeHostWebApiProject.ProjectName}{NamingStats.JsonExtension}");

        const string connectionStringParameterName = "ConnectionStringSeed";
        var projectDesignTimeDbContextFactoryCreator = new FakeProjectDesignTimeDbContextFactoryCreator(Logger,
            fakeHosProjectPath, _par.DbContextProjectName, _scaffoldSeederCreatorData.FakeHostWebApiProject.ProjectName,
            _par.ProjectDbContextClassName, connectionStringParameterName, parametersFileName, _par.ScaffoldSeederProjectName);

        projectDesignTimeDbContextFactoryCreator.CreateFileStructure();

        var fakeHostProjectParameters = new FakeHostProjectParametersDomain(_par.DevDatabaseDataProvider,
            $"{_par.DevDatabaseConnectionString.AddNeedLastPart(';')}Application Name={NamingStats.SeedDbProjectName(_par.ScaffoldSeederProjectName)}");

        //seederParameters შევინახოთ json-ის სახით პარამეტრების ფოლდერში შესაბამისი პროექტისათვის
        string paramsJsonText = JsonConvert.SerializeObject(fakeHostProjectParameters, Formatting.Indented);

        //აქ შეიძლება დაშიფვრა დაგვჭირდეს.
        await File.WriteAllTextAsync(parametersFileName, paramsJsonText, cancellationToken);

        string? migrationSqlFilesFolder = _par.MigrationSqlFilesFolder;
        //თუ მიგრაციის sql ფაილების ფოლდერი მითითებულია პარამეტრებში, ეს ფოლდერი არსებობს და შეიცავს ერთს მაინც *.sql ფაილს,
        if (string.IsNullOrWhiteSpace(migrationSqlFilesFolder) || !Directory.Exists(migrationSqlFilesFolder))
        {
            return true;
        }

        var sqlDir = new DirectoryInfo(migrationSqlFilesFolder);
        FileInfo[] sqlFiles = sqlDir.GetFiles("*.sql");
        if (sqlFiles.Length == 0)
        {
            return true;
        }

        string projectPath = Path.Combine(SolutionPath, _scaffoldSeederCreatorData.DbMigrationProject.ProjectName);
        string projectFileFullName = Path.Combine(projectPath,
            $"{_scaffoldSeederCreatorData.DbMigrationProject.ProjectName}.csproj");
        string migrationProjectSqlFilesFolderPath = Path.Combine(projectPath, "Sql");

        //მაშინ მიგრაციის პროექტის ფოლდერში დაემატოს Sql ფოლდერი და მასში დაკოპირდეს *.sql ფაილები migrationSqlFilesFolder ფოლდერიდან
        //მიგრაციის პროექტის ფაილში <ItemGroup>-ის შიგნით თითოეული *.sql ფაილისთვის უნდა გაკეთდეს ასეთი ჩანაწერი
        //  <ItemGroup>
        //    <EmbeddedResource Include="Sql\Sp_GetAllbatchesByStatus.sql" />
        //  </ItemGroup>
        if (sqlFiles.Select(sqlFile => sqlFile.CopyTo(Path.Combine(migrationProjectSqlFilesFolderPath, sqlFile.Name)))
            .All(newFile => RegisterEmbeddedResource(projectFileFullName, newFile.Name)))
        {
            return true;
        }

        StShared.WriteErrorLine("EmbeddedResource does not Registered", true);
        return false;
    }

    private static bool RegisterEmbeddedResource(string projectFileFullName, string sqlFileName)
    {
        XElement projectXml = XElement.Load(projectFileFullName);

        XElement? firstItemGroup = projectXml.Descendants("ItemGroup").FirstOrDefault();
        if (firstItemGroup == null)
        {
            projectXml.Add(new XElement("ItemGroup"));
        }

        firstItemGroup = projectXml.Descendants("ItemGroup").FirstOrDefault();
        if (firstItemGroup == null)
        {
            StShared.WriteErrorLine("ItemGroup does not created", true);
            return false;
        }

        firstItemGroup.Add(new XElement("EmbeddedResource", new XAttribute("Include", $"Sql\\{sqlFileName}")));

        projectXml.Save(projectFileFullName);

        return true;
    }

    //სოლუშენის აწყობის შემდეგ გასაკეთებელი საქმეები
    //1.  უნდა გაეშვას სკაფოლდინგის პროცესი, რომელიც რეალური ბაზის ასლიდან დაამზადებს მონაცემთა ბაზის კონტექსტს (DbContext)
    //#dotnet ef dbcontext scaffold "$ConnectionStringProd" Microsoft.EntityFrameworkCore.SqlServer --startup-project "..\$CreateProjectSeederCodeProjectName\$CreateProjectSeederCodeProjectName.csproj" --context $DbScContextName --context-dir . --output-dir $modelsFolderName -f
    //dotnet ef dbcontext scaffold "$ConnectionStringProd" Microsoft.EntityFrameworkCore.SqlServer --startup-project $projects[$CreateProjectSeederCodeProjectName] --context $DbScContextName --context-dir . --output-dir $modelsFolderName -f --no-pluralize

    //1a. #ზედმეტი OnConfigured მეთოდის წაშლა ახლად დაგენერირებული DbContext კლასიდან
    //Write-Host "300 remove OnConfigured method from generated DbContext"
    //RemoveOnConfigured "$DbScContextName.cs"

    //2. შეიქმნას კონსოლ აპლიკაციების პარამეტრების ფაილები, რომლებიც უნდა ჩაიწეროს სპეციალურად შექმნილ პარამეტრების .sec ფოლდერში

    //3. _seedDbProject-ისთვის dotnet user-secrets init არ ვიცი რამდენად საჭიროა.

    //4. 
}
