using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class DbContextForCarcassClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DbContextForCarcassClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty,
            "using System",
            "using System.Linq",
            "using CarcassDb",
            "using CarcassDb.Observers",
            $"using {_projectNamespace}Db.Models",
            $"using {_projectNamespace}Db.QueryModels",
            "using Microsoft.EntityFrameworkCore",
            "using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal",
            "using SystemToolsShared",
            string.Empty,
            $"namespace {_projectNamespace}Db",
            string.Empty,
            new CodeBlock($"public sealed class {_projectNamespace}DbContext : CarcassDbContext",
                string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}DbContext(DbContextOptions<{_projectNamespace}DbContext> options, IDataObserversManager dataObserversManager) : base(ChangeOptionsType<CarcassDbContext>(options), dataObserversManager)",
                    new OneLineComment($"Console.WriteLine(\"{_projectNamespace}DbContext Constructor 1...\");")),
                string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}DbContext(DbContextOptions<{_projectNamespace}DbContext> options, bool isDesignTime) : base(options, isDesignTime)",
                    new OneLineComment($"Console.WriteLine(\"{_projectNamespace}DbContext Constructor 2...\");")),
                string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}DbContext(DbContextOptions<{_projectNamespace}DbContext> options, int int1) : base(ChangeOptionsType<CarcassDbContext>(options), int1)",
                    new OneLineComment($"Console.WriteLine(\"{_projectNamespace}DbContext Constructor 3...\");")),
                string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}DbContext(DbContextOptions<{_projectNamespace}DbContext> options) : base(ChangeOptionsType<CarcassDbContext>(options))",
                    new OneLineComment($"Console.WriteLine(\"{_projectNamespace}DbContext Constructor 4...\");")),
                string.Empty,
                new CodeBlock(
                    "private static DbContextOptions<T> ChangeOptionsType<T>(DbContextOptions options) where T : DbContext",
                    new OneLineComment(
                        $"Console.WriteLine(\"{_projectNamespace}DbContext ChangeOptionsType Start...\");"),
                    string.Empty,
                    string.Empty,
                    "var sqlExt = options.Extensions.FirstOrDefault(e => e is SqlServerOptionsExtension)",
                    string.Empty,
                    new OneLineComment(
                        $"Console.WriteLine(\"{_projectNamespace}DbContext ChangeOptionsType Pass 1...\");"),
                    string.Empty,
                    new CodeBlock("if (sqlExt == null)",
                        "throw new Exception(\"Failed to retrieve SQL connection string for base Context\")",
                        string.Empty),
                    "string? connectionString = ((SqlServerOptionsExtension)sqlExt).ConnectionString",
                    new CodeBlock("if (connectionString == null)",
                        "throw new Exception(\"Connection string for base Context dos not specified\")",
                        string.Empty),
                    new OneLineComment(
                        $"Console.WriteLine(\"{_projectNamespace}DbContext ChangeOptionsType Pass 2...\");"),
                    string.Empty,
                    "return new DbContextOptionsBuilder<T>().UseSqlServer(connectionString).EnableSensitiveDataLogging().Options"),
                string.Empty,
                new OneLineComment("ბაზაში არსებული ცხრილები წარმოდგენილი DbSet-ების სახით"),
                new OneLineComment(
                    " public virtual DbSet<ActantCombination> ActantCombinations => Set<ActantCombination>();"),
                string.Empty,
                new CodeBlock("protected override void OnModelCreating(ModelBuilder modelBuilder)",
                    new OneLineComment(
                        $"Console.WriteLine(\"{_projectNamespace}DbContext OnModelCreating Start...\");"),
                    string.Empty,
                    "base.OnModelCreating(modelBuilder)",
                    string.Empty,
                    new OneLineComment(
                        $"Console.WriteLine(\"{_projectNamespace}DbContext OnModelCreating Pass 1...\");"),
                    string.Empty,
                    new OneLineComment("აქედან უნდა დაემატოს მოდელის იმ ნაწილის შესახებ,"),
                    new OneLineComment("რომელიც განკუთვნილია მხოლოდ ამ კონკრეტული ბაზისთვის და არა კარკასისათვის"),
                    string.Empty,
                    "BuildMain(modelBuilder)",
                    string.Empty,
                    new OneLineComment(
                        $"Console.WriteLine(\"{_projectNamespace}DbContext OnModelCreating Pass 2...\");"),
                    string.Empty,
                    new OneLineComment("Queries"),
                    "BuildQueries(modelBuilder)",
                    string.Empty,
                    new OneLineComment(
                        $"Console.WriteLine(\"{_projectNamespace}DbContext OnModelCreating Pass 3...\");")),
                string.Empty,
                new CodeBlock("private void BuildMain(ModelBuilder modelBuilder)",
                    string.Empty,
                    new OneLineComment("ბაზაში არსებული ცხრილის აღწერის ნიმუში"),
                    "modelBuilder.Entity<TestModel>(entity => { string tableName = nameof(TestModel).Pluralize(); entity.HasKey(e => e.TestId); entity.ToTable(tableName.UnCapitalize()); entity.HasIndex(e => e.TestName).HasDatabaseName($\"IX_{tableName}_{nameof(TestModel.TestName).UnCapitalize()}\").IsUnique(); entity.Property(e => e.TestId).HasColumnName(nameof(TestModel.TestId).UnCapitalize()); entity.Property(e => e.TestName).HasColumnName(nameof(TestModel.TestName).UnCapitalize()).HasMaxLength(50); })",
                    string.Empty),
                new CodeBlock("private void BuildQueries(ModelBuilder modelBuilder)",
                    new OneLineComment("ჩასატვირთი ქვერის აღწერის ნიმუში"),
                    new OneLineComment(" modelBuilder.Entity<AfterDominantPersonMarkerQuery>(entity =>"),
                    new OneLineComment(" {"),
                    new OneLineComment("     entity.HasNoKey();"),
                    new OneLineComment("     //entity.ToTable(\"\");"),
                    new OneLineComment("     entity.Property(e => e.VerbTypeId).HasColumnName(\"verbTypeId\");"),
                    new OneLineComment("     entity.Property(e => e.VerbSeriesId).HasColumnName(\"verbSeriesId\");"),
                    new OneLineComment(
                        "     entity.Property(e => e.ActantCombinationId).HasColumnName(\"actantCombinationId\");"),
                    new OneLineComment(
                        "     entity.Property(e => e.DominantActantId).HasColumnName(\"dominantActantId\");"),
                    new OneLineComment("     entity.Property(e => e.ActantGroupId).HasColumnName(\"actantGroupId\");"),
                    new OneLineComment("     entity.Property(e => e.VerbNumberId).HasColumnName(\"verbNumberId\");"),
                    new OneLineComment("     entity.Property(e => e.VerbPersonId).HasColumnName(\"verbPersonId\");"),
                    new OneLineComment(" });"),
                    string.Empty,
                    string.Empty),
                string.Empty));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}