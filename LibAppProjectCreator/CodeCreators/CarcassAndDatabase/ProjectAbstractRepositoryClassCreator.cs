using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators.CarcassAndDatabase;

public sealed class ProjectAbstractRepositoryClassCreator : CodeCreator
{
    private readonly string _projectName;
    private readonly string _projectShortName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectAbstractRepositoryClassCreator(ILogger logger, string placePath, string projectName,
        string projectShortName, string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectName = projectName;
        _projectShortName = projectShortName;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, $"using {_projectName}Db", "using RepositoriesDom", string.Empty,
            $"namespace {_projectName}Repositories", string.Empty,
            $"public sealed class {_projectShortName.Capitalize()}AbstractRepository({_projectName}DbContext ctx) : AbstractRepository(ctx)");
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}