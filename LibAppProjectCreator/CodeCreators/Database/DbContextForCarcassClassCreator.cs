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
            "using Microsoft.EntityFrameworkCore",
            "using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal",
            "using System",
            "using System.Linq",
            string.Empty,
            "using CarcassDb",
            $"using {_projectNamespace}Db.Models",
            string.Empty,
            $"namespace {_projectNamespace}Db",
            string.Empty,
            new CodeBlock($"public sealed class {_projectNamespace}DbContext : CarcassDbContext",
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
                    "var sqlExt = options.Extensions.FirstOrDefault(e => e is SqlServerOptionsExtension) ?? throw new Exception(\"Failed to retrieve SQL connection string for base Context\")",
                    "var connectionString = ((SqlServerOptionsExtension)sqlExt).ConnectionString ?? throw new Exception(\"Connection string for base Context dos not specified\")",
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
                    "modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly)",
                string.Empty)));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}