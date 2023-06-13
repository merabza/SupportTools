using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class RepositoryClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public RepositoryClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            //"using System.Collections.Generic",
            //"using System.Linq",
            //"using System.Net",
            $"using {_projectNamespace}Db",
            //$"using {_projectNamespace}Db.Models",
            //"using Microsoft.EntityFrameworkCore",
            "using Microsoft.EntityFrameworkCore.Storage",
            "using Microsoft.Extensions.Logging",
            "",
            $"namespace Lib{_projectNamespace}Repositories",
            "",
            new CodeBlock($"public sealed class {_projectNamespace}Repository : I{_projectNamespace}Repository",
                $"private readonly {_projectNamespace}DbContext _context",
                $"private readonly ILogger<{_projectNamespace}Repository> _logger",
                "",
                new CodeBlock(
                    $"public {_projectNamespace}Repository({_projectNamespace}DbContext ctx, ILogger<{_projectNamespace}Repository> logger)",
                    "_context = ctx",
                    "_logger = logger"
                ),
                new CodeBlock("public int SaveChanges()",
                    new CodeBlock("try",
                        "return _context.SaveChanges()"
                    ),
                    new CodeBlock("catch (Exception e)",
                        "_logger.LogError(e, $\"Error occurred executing {nameof(SaveChanges)}.\")",
                        "throw"
                    )
                ),
                "",
                new CodeBlock("public int SaveChangesWithTransaction()",
                    new CodeBlock("try",
                        "using IDbContextTransaction transaction = GetTransaction()",
                        new CodeBlock("try",
                            "int ret = _context.SaveChanges()",
                            "transaction.Commit()",
                            "return ret"
                        ),
                        new CodeBlock("catch (Exception)",
                            "transaction.Rollback()",
                            "throw"
                        )
                    ),
                    new CodeBlock("catch (Exception e)",
                        "_logger.LogError(e, $\"Error occurred executing {nameof(SaveChangesWithTransaction)}.\")",
                        "throw"
                    )
                ),
                new CodeBlock("public IDbContextTransaction GetTransaction()",
                    "return _context.Database.BeginTransaction()"
                )
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}