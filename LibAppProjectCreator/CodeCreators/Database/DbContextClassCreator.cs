using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class DbContextClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DbContextClassCreator(ILogger logger, string placePath, string projectNamespace, string? codeFileName = null)
        : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, $"using {_projectNamespace}Db.Models", "using SystemToolsShared",
            "using Microsoft.EntityFrameworkCore", string.Empty, $"namespace {_projectNamespace}Db", string.Empty,
            new CodeBlock($"public sealed class {_projectNamespace}DbContext : DbContext", string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}DbContext(DbContextOptions options, bool isDesignTime) : base(options)",
                    string.Empty), string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}DbContext(DbContextOptions<{_projectNamespace}DbContext> options) : base(options)",
                    string.Empty),
                new CodeBlock("protected override void OnModelCreating(ModelBuilder modelBuilder)", string.Empty,
                    "modelBuilder.Entity<TestModel>(entity => { string tableName = nameof(TestModel).Pluralize(); entity.HasKey(e => e.TestId); entity.ToTable(tableName.UnCapitalize()); entity.HasIndex(e => e.TestName).HasDatabaseName($\"IX_{tableName}_{nameof(TestModel.TestName).UnCapitalize()}\").IsUnique(); entity.Property(e => e.TestId).HasColumnName(nameof(TestModel.TestId).UnCapitalize()); entity.Property(e => e.TestName).HasColumnName(nameof(TestModel.TestName).UnCapitalize()).HasMaxLength(50); })",
                    string.Empty)));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}