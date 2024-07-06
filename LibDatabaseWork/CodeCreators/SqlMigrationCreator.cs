using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.CodeCreators;

public sealed class SqlMigrationCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SqlMigrationCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using Microsoft.EntityFrameworkCore.Migrations",
            "using System.IO",
            "using System.Linq",
            "using System.Reflection",
            string.Empty,
            $"namespace {_projectNamespace}.Migrations",
            string.Empty,
            new CodeBlock("public sealed partial class Sql : Migration",
                string.Empty,
                new CodeBlock("protected override void Up(MigrationBuilder migrationBuilder)",
                    "var assembly = Assembly.GetExecutingAssembly()",
                    "var sqlFiles = assembly.GetManifestResourceNames().Where(file => file.EndsWith(\".sql\"))",
                    new CodeBlock("foreach (var sqlFile in sqlFiles)",
                        "using var stream = assembly.GetManifestResourceStream(sqlFile)",
                        "if (stream is null) continue",
                        "using var reader = new StreamReader(stream)",
                        "var sqlScript = reader.ReadToEnd()",
                        "migrationBuilder.Sql($\"EXEC(N'{sqlScript}')\")")),
                new CodeBlock("protected override void Down(MigrationBuilder migrationBuilder)", string.Empty)));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}