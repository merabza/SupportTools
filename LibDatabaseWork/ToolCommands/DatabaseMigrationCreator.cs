using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.CodeCreators;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibDatabaseWork.ToolCommands;

public sealed class DatabaseMigrationCreator : MigrationToolCommand
{
    private const string ActionName = "Database Migration Initial";
    private const string ActionDescription = "Database Migration Initial";
    private readonly ILogger _logger;

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseMigrationCreator(ILogger logger, DatabaseMigrationParameters databaseMigrationParameters,
        IParametersManager? parametersManager) : base(logger, ActionName, databaseMigrationParameters,
        parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //მიგრაციის პროექტის ფაილი რომელ ფოლდერშიც არის, იმავეში უნდა იყოს Migrations ფოლდერი.
        //თუ ეს ფოლდერი აღმოჩენილი არ იქნება, გამოვიდეს შენიშვნა ამის შესახებ
        //თუ Migrations ფოლდერი არის, მაშინ უნდა მოხდეს მისი შიგთავსის გასუფთავება *.cs ფაილებისაგან
        var migrationProjectFile = new FileInfo(DatabaseMigrationParameters.MigrationProjectFileName);
        if (migrationProjectFile.Directory == null)
        {
            _logger.LogError("Object for Migration project file directory cannot be created");
            return ValueTask.FromResult(false);
        }

        var migrationsFolder = migrationProjectFile.Directory.GetDirectories("Migrations").SingleOrDefault();
        if (migrationsFolder is not null)
        {
            var directorySeparatorChar = Path.DirectorySeparatorChar;
            _logger.LogInformation("Delete Migrations{directorySeparatorChar}*.cs files", directorySeparatorChar);
            foreach (var csFile in migrationsFolder.GetFiles("*.cs"))
                csFile.Delete();

        }   
        _logger.LogInformation("Create Initial Migration");

        //ბაზის მიგრაციის დაწყება
        if (StShared.RunProcess(true, _logger, "dotnet",
                $"ef migrations add \"Initial\" --context {DatabaseMigrationParameters.DbContextName} --startup-project {DatabaseMigrationParameters.MigrationStartupProjectFilePath} --project {DatabaseMigrationParameters.MigrationProjectFileName}")
            .IsSome)
            return ValueTask.FromResult(false);

        _logger.LogInformation("Update Database for Initial");
        //ბაზის განახლება
        if (StShared.RunProcess(true, _logger, "dotnet",
                $"ef database update --context {DatabaseMigrationParameters.DbContextName} --startup-project {DatabaseMigrationParameters.MigrationStartupProjectFilePath} --project {DatabaseMigrationParameters.MigrationProjectFileName}")
            .IsSome)
            return ValueTask.FromResult(false);

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

        var sqlFolder = migrationProjectFile.Directory.GetDirectories("Sql").SingleOrDefault();
        if (sqlFolder == null)
            return ValueTask.FromResult(true);

        _logger.LogInformation("Create sql Migration");
        //ბაზის მიგრაციის დაწყება
        if (StShared.RunProcess(true, _logger, "dotnet",
                $"ef migrations add \"Sql\" --context {DatabaseMigrationParameters.DbContextName} --startup-project {DatabaseMigrationParameters.MigrationStartupProjectFilePath} --project {DatabaseMigrationParameters.MigrationProjectFileName}")
            .IsSome)
            return ValueTask.FromResult(false);

        var sqlMigrationFile = migrationsFolder.GetFiles("??????????????_Sql.cs").SingleOrDefault();
        if (sqlMigrationFile == null)
        {
            _logger.LogError("sql Migration File Not found");
            return ValueTask.FromResult(false);
        }

        var sqlMigrationCreator = new SqlMigrationCreator(_logger, migrationsFolder.FullName,
            Path.GetFileNameWithoutExtension(DatabaseMigrationParameters.MigrationProjectFileName),
            sqlMigrationFile.Name);

        sqlMigrationCreator.CreateFileStructure();

        _logger.LogInformation("Update Database for sql Migration");
        //ბაზის განახლება
        return ValueTask.FromResult(StShared.RunProcess(true, _logger, "dotnet",
                $"ef database update --context {DatabaseMigrationParameters.DbContextName} --startup-project {DatabaseMigrationParameters.MigrationStartupProjectFilePath} --project {DatabaseMigrationParameters.MigrationProjectFileName}")
            .IsNone);
    }
}