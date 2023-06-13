using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.CodeCreators;

public sealed class SqlMigrationCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public SqlMigrationCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using Microsoft.EntityFrameworkCore.Migrations",
            "using System.IO",
            "using System.Linq",
            "using System.Reflection",
            "",
            $"namespace {_projectNamespace}.Migrations",
            "",
            new CodeBlock("public partial sealed class Sql : Migration",
                "",
                new CodeBlock("protected override void Up(MigrationBuilder migrationBuilder)",
                    "var assembly = Assembly.GetExecutingAssembly()",
                    "var sqlFiles = assembly.GetManifestResourceNames().Where(file => file.EndsWith(\".sql\"))",
                    new CodeBlock("foreach (var sqlFile in sqlFiles)",
                        new CodeBlock(
                            "using (Stream stream = assembly.GetManifestResourceStream(sqlFile)) using (StreamReader reader = new StreamReader(stream))",
                            "var sqlScript = reader.ReadToEnd()",
                            "migrationBuilder.Sql($\"EXEC(N'{sqlScript}')\")"))),
                new CodeBlock("protected override void Down(MigrationBuilder migrationBuilder)", "")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}