using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class ProjectDesignTimeDbContextFactoryClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public ProjectDesignTimeDbContextFactoryClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "",
            $"using {_projectNamespace}Db",
            "",
            $"namespace {_projectNamespace}",
            "",
            new OneLineComment("ეს კლასი საჭიროა იმისათვის, რომ შესაძლებელი გახდეს მიგრაციასთან მუშაობა."),
            new OneLineComment(
                "ანუ დეველოპერ ბაზის წაშლა და ახლიდან დაგენერირება, ან მიგრაციაში ცვლილებების გაკეთება"),
            new CodeBlock(
                $"public sealed class {_projectNamespace}DesignTimeDbContextFactory : DesignTimeDbContextFactory<{_projectNamespace}DbContext>",
                new CodeBlock(
                    $"public {_projectNamespace}DesignTimeDbContextFactory() : base(\"{_projectNamespace}DbMigration\", \"ConnectionString\", \"D:\\\\1WorkSecurity\\\\{_projectNamespace}\\\\{_projectNamespace}.json\")",
                    ""
                )));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}