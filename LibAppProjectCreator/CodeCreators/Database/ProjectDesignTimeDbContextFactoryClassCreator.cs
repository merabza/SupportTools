using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class ProjectDesignTimeDbContextFactoryClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectDesignTimeDbContextFactoryClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty,
            $"using {_projectNamespace}Db",
            string.Empty,
            $"namespace {_projectNamespace}",
            string.Empty,
            new OneLineComment("ეს კლასი საჭიროა იმისათვის, რომ შესაძლებელი გახდეს მიგრაციასთან მუშაობა."),
            new OneLineComment("ანუ დეველოპერ ბაზის წაშლა და ახლიდან დაგენერირება, ან მიგრაციაში ცვლილებების გაკეთება"),
            new OneLineComment(" ReSharper disable once UnusedType.Global"),
            new CodeBlock(
                $"public sealed class {_projectNamespace}DesignTimeDbContextFactory : DesignTimeDbContextFactory<{_projectNamespace}DbContext>",
                new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
                new CodeBlock(
                    $"public {_projectNamespace}DesignTimeDbContextFactory() : base(\"{_projectNamespace}DbMigration\", \"ConnectionString\", @\"D:\\1WorkSecurity\\{_projectNamespace}\\{_projectNamespace}.json\")",
                    string.Empty
                )));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}