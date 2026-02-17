using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibDatabaseWork.CodeCreators;
using LibDatabaseWork.Models;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibDatabaseWork.ToolCommands;

public sealed class DatabaseMigrationCreatorMigrationToolCommand : MigrationToolCommand
{
    private const string ActionName = "Database Migration Initial";
    private const string ActionDescription = "Database Migration Initial";
    private readonly ILogger _logger;

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseMigrationCreatorMigrationToolCommand(ILogger logger,
        DatabaseMigrationParameters databaseMigrationParameters, IParametersManager? parametersManager) : base(logger,
        ActionName, databaseMigrationParameters, parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //მიგრაციის პროექტის ფაილი რომელ ფოლდერშიც არის, იმავეში უნდა იყოს Migrations ფოლდერი.
        //თუ ეს ფოლდერი აღმოჩენილი არ იქნება, გამოვიდეს შენიშვნა ამის შესახებ
        //თუ Migrations ფოლდერი არის, მაშინ უნდა მოხდეს მისი შიგთავსის გასუფთავება *.cs ფაილებისაგან

        string migrationProjectFileName = DatabaseMigrationParameters.MigrationProjectFileName;

        var migrationProjectFile = new FileInfo(migrationProjectFileName);
        if (!migrationProjectFile.Exists)
        {
            _logger.LogError("Object for Migration project file {MigrationProjectFileName} does not exists",
                migrationProjectFileName);
            return ValueTask.FromResult(false);
        }

        DirectoryInfo? migrationsFolder =
            migrationProjectFile.Directory?.GetDirectories("Migrations").SingleOrDefault();
        if (migrationsFolder is not null && migrationsFolder.Exists)
        {
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Delete Migrations{DirectorySeparatorChar}*.cs files", directorySeparatorChar);
            }

            foreach (FileInfo csFile in migrationsFolder.GetFiles("*.cs"))
            {
                csFile.Delete();
            }
        }

        string migrationStartupProjectFilePath = DatabaseMigrationParameters.MigrationStartupProjectFilePath;
        var migrationStartupProjectFile = new FileInfo(migrationStartupProjectFilePath);
        if (!migrationStartupProjectFile.Exists)
        {
            _logger.LogError("Object for Migration startup project file {MigrationStartupProjectFile} does not exists",
                migrationStartupProjectFile);
            return ValueTask.FromResult(false);
        }

        //var solutionFileNameWithMigrationProject = DatabaseMigrationParameters.SolutionFileNameWithMigrationProject;
        //var solutionFileName = new FileInfo(solutionFileNameWithMigrationProject);
        //if (!solutionFileName.Exists)
        //{
        //    _logger.LogError(
        //        "Object for Migration startup project file {solutionFileNameWithMigrationProject} does not exists",
        //        solutionFileNameWithMigrationProject);
        //    return ValueTask.FromResult(false);
        //}

        XElement projectXml = XElement.Load(migrationStartupProjectFilePath);
        IEnumerable<XElement> projectReferences = projectXml.Descendants("ItemGroup").Descendants("ProjectReference");

        string? migrationStartupProjectDirectoryName = migrationStartupProjectFile.DirectoryName;
        if (string.IsNullOrWhiteSpace(migrationStartupProjectDirectoryName))
        {
            _logger.LogError(
                "migrationStartupProjectFile.DirectoryName for Migration project file {MigrationProjectFileName} does not exists",
                migrationProjectFileName);
            return ValueTask.FromResult(false);
        }

        bool migrationProjectFileExistsInStartupProjectReferences = false;
        foreach (XElement projectReference in projectReferences)
        {
            if (projectReference.Attributes().Where(w => w.Name == "Include").Any(w => migrationProjectFileName ==
                    Path.GetFullPath(Path.Combine(migrationStartupProjectDirectoryName, w.Value))))
            {
                migrationProjectFileExistsInStartupProjectReferences = true;
            }
        }

        var dotnetProcessor = new DotnetProcessor(_logger, true);

        if (!migrationProjectFileExistsInStartupProjectReferences)
        {
            dotnetProcessor.AddReferenceToProject(migrationStartupProjectFilePath, migrationProjectFileName);
        }

        _logger.LogInformation("Create Initial Migration");
        string dbContextName = DatabaseMigrationParameters.DbContextName;

        //ბაზის მიგრაციის დაწყება
        if (dotnetProcessor.EfAddDatabaseMigration("Initial", dbContextName, migrationStartupProjectFilePath,
                migrationProjectFileName).IsSome)
        {
            return ValueTask.FromResult(false);
        }

        _logger.LogInformation("Update Database for Initial");
        //ბაზის განახლება
        if (dotnetProcessor.EfUpdateDatabaseByMigration(dbContextName,
                migrationStartupProjectFilePath, migrationProjectFileName).IsSome)
        {
            return ValueTask.FromResult(false);
        }

        //იმისათვის, რომ ამ კოდმა იმუშავოს, საჭიროა შემდეგი:
        //1. მიგრაციის პროექტში უნდა დაემატოს ფოლდერი Sql
        //2. Sql ფოლდერში უნდა დაემატოს *.sql ფაილები, რომლებშიც იქნება სკრიპტები პროცედურების ფუნქციების და ასე შემდეგ ბაზის სერვერის მხარეს შესაქმნელად
        //3. მიგრაციის პროექტის ფაილში <ItemGroup>-ის შიგნით თითოეული *.sql ფაილისთვის უნდა გაკეთდეს ასეთი ჩანაწერი
        //  <ItemGroup>
        //    <EmbeddedResource Include="Sql\Sp_GetAllbatchesByStatus.sql" />
        //  </ItemGroup>
        //აქ ყურადღება უნდა მიექცეს იმას, რომ ფაილის სახელის წინ უნდა იყოს Sql\ 
        //რაც საჭიროა იმისათვის, რომ მიგრაციის კოდმა ფაილები ამოიღოს Sql ფოლდერიდან
        //4. სასურველია თითოული ფაილი თითო სტორედ პროცედურას, ან ფუნქციას ემსახურებოდეს. რომ მომავალში ადვილი გახდეს მათი რედაქტირება, დამატება ან წაშლა.
        //5. შექმნის ბრძანება სასურველია იყოს CREATE OR ALTER 

        DirectoryInfo? sqlFolder = migrationProjectFile.Directory?.GetDirectories("Sql").SingleOrDefault();
        if (sqlFolder == null)
        {
            return ValueTask.FromResult(true);
        }

        _logger.LogInformation("Create sql Migration");
        //ბაზის sql მიგრაციის დაწყება
        if (dotnetProcessor
            .EfAddDatabaseMigration("Sql", dbContextName, migrationStartupProjectFilePath, migrationProjectFileName)
            .IsSome)
        {
            return ValueTask.FromResult(false);
        }

        FileInfo? sqlMigrationFile = migrationsFolder?.GetFiles("??????????????_Sql.cs").SingleOrDefault();
        if (sqlMigrationFile == null)
        {
            _logger.LogError("sql Migration File Not found");
            return ValueTask.FromResult(false);
        }

        migrationsFolder = migrationProjectFile.Directory?.GetDirectories("Migrations").SingleOrDefault();
        if (migrationsFolder is null)
        {
            _logger.LogError("Migrations Folder Not found");
            return ValueTask.FromResult(false);
        }

        var sqlMigrationCreator = new SqlMigrationCreator(_logger, migrationsFolder.FullName,
            Path.GetFileNameWithoutExtension(migrationProjectFileName), sqlMigrationFile.Name);

        sqlMigrationCreator.CreateFileStructure();

        _logger.LogInformation("Update Database for sql Migration");
        //ბაზის განახლება
        return ValueTask.FromResult(dotnetProcessor
            .EfUpdateDatabaseByMigration(dbContextName, migrationStartupProjectFilePath, migrationProjectFileName)
            .IsNone);
    }
}
