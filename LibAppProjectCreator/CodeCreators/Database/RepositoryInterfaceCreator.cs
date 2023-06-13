using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class RepositoryInterfaceCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public RepositoryInterfaceCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            //"using System.Collections.Generic",
            //"using System.Net",
            //$"using {_projectNamespace}Db.Models",
            "using Microsoft.EntityFrameworkCore.Storage",
            "",
            $"namespace Lib{_projectNamespace}Repositories",
            "",
            new CodeBlock($"public interface I{_projectNamespace}Repository",
                "int SaveChanges()",
                "int SaveChangesWithTransaction()",
                "IDbContextTransaction GetTransaction()"
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}