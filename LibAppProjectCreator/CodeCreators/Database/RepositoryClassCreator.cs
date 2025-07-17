using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class RepositoryClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RepositoryClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System", $"using {_projectNamespace}Db", "using Microsoft.EntityFrameworkCore.Storage",
            "using Microsoft.Extensions.Logging", string.Empty, $"namespace Lib{_projectNamespace}Repositories",
            string.Empty,
            new CodeBlock($"public sealed class {_projectNamespace}Repository : I{_projectNamespace}Repository",
                $"private readonly {_projectNamespace}DbContext _context",
                $"private readonly ILogger<{_projectNamespace}Repository> _logger", string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}Repository({_projectNamespace}DbContext ctx, ILogger<{_projectNamespace}Repository> logger)",
                    "_context = ctx", "_logger = logger"),
                new CodeBlock("public int SaveChanges()", new CodeBlock("try", "return _context.SaveChanges()"),
                    new CodeBlock("catch (Exception e)",
                        "_logger.LogError(e, $\"Error occurred executing {nameof(SaveChanges)}.\")", "throw")),
                string.Empty,
                new CodeBlock("public int SaveChangesWithTransaction()",
                    new CodeBlock("try", "using IDbContextTransaction transaction = GetTransaction()",
                        new CodeBlock("try", "int ret = _context.SaveChanges()", "transaction.Commit()", "return ret"),
                        new CodeBlock("catch (Exception)", "transaction.Rollback()", "throw")),
                    new CodeBlock("catch (Exception e)",
                        "_logger.LogError(e, $\"Error occurred executing {nameof(SaveChangesWithTransaction)}.\")",
                        "throw")),
                new CodeBlock("public IDbContextTransaction GetTransaction()",
                    "return _context.Database.BeginTransaction()")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}