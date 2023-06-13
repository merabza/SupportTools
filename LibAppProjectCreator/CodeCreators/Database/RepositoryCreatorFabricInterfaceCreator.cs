using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class RepositoryCreatorFabricInterfaceCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public RepositoryCreatorFabricInterfaceCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            $"namespace Lib{_projectNamespace}Repositories",
            "",
            new CodeBlock($"public interface I{_projectNamespace}RepositoryCreatorFabric",
                $"I{_projectNamespace}Repository Get{_projectNamespace}Repository()"
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}