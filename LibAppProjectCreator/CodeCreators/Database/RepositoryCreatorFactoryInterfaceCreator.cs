using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class RepositoryCreatorFactoryInterfaceCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RepositoryCreatorFactoryInterfaceCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            $"namespace Lib{_projectNamespace}Repositories", string.Empty,
            new CodeBlock($"public interface I{_projectNamespace}RepositoryCreatorFactory",
                $"I{_projectNamespace}Repository Get{_projectNamespace}Repository()"));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}