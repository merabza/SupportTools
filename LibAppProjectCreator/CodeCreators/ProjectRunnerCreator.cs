using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectRunnerCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectRunnerCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, "using CliParameters.Tasks",
            $"using {(_useDatabase ? "Do" : string.Empty)}{_projectNamespace}.Models",
            "using Microsoft.Extensions.Logging", "using System", "using SystemToolsShared",
            _useDatabase ? $"using Lib{_projectNamespace}Repositories" : string.Empty, string.Empty,
            //new CodeBlock($"namespace Do{_projectNamespace}",
            $"namespace {(_useDatabase ? "Do" : string.Empty)}{_projectNamespace}", string.Empty, new CodeBlock(
                $"public sealed class {_projectNamespace}Runner : TaskRunner", "private readonly ILogger _logger",
                _useDatabase
                    ? $"private readonly I{_projectNamespace}RepositoryCreatorFactory _{_projectNamespace.UnCapitalize()}RepositoryCreatorFactory"
                    : null, $"private readonly {_projectNamespace}Parameters _par", string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}Runner(ILogger logger{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFactory {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory" : null)}, {_projectNamespace}Parameters par)",
                    "_logger = logger",
                    _useDatabase
                        ? $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFactory = {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory"
                        : null, "_par = par"), string.Empty, new CodeBlock(
                    "public override bool Run()", //{(_taskWithParameters ? $"{_projectNamespace}TaskModel task" : string.Empty)}
                    new CodeBlock("try", new OneLineComment("ამოცანის შესრულება იწყება აქ"), string.Empty,
                        "return true"),
                    new CodeBlock("catch (Exception e)", "StShared.WriteException(e, true)", "throw"))));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}