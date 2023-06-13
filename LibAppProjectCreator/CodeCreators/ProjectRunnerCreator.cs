using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectRunnerCreator : CodeCreator
{
    private readonly string _projectNamespace;
    //      private readonly bool _tasksWithParameters;

    private readonly bool _useDatabase;
    //private readonly bool _taskWithParameters;

    //, bool taskWithParameters
    public ProjectRunnerCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
//            _tasksWithParameters = tasksWithParameters;
        _useDatabase = useDatabase;
        //_taskWithParameters = taskWithParameters;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "",
            "using CliParameters.Tasks",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            //$"using Do{_projectNamespace}.Models",
            "using Microsoft.Extensions.Logging",
            "using System",
            "using SystemToolsShared",
            _useDatabase ? $"using Lib{_projectNamespace}Repositories" : "",
            "",
            //new CodeBlock($"namespace Do{_projectNamespace}",
            $"namespace {(_useDatabase ? "Do" : "")}{_projectNamespace}",
            "",
            new CodeBlock(
                $"public sealed class {_projectNamespace}Runner : TaskRunner",
                "private readonly ILogger _logger",
                _useDatabase
                    ? $"private readonly I{_projectNamespace}RepositoryCreatorFabric _{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric"
                    : null,
                $"private readonly {_projectNamespace}Parameters _par",
                "",
                new CodeBlock(
                    $"public {_projectNamespace}Runner(ILogger logger{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFabric {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric" : null)}, {_projectNamespace}Parameters par)",
                    "_logger = logger",
                    _useDatabase
                        ? $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric = {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric"
                        : null,
                    "_par = par"),
                "",
                new CodeBlock(
                    "public override bool Run()", //{(_taskWithParameters ? $"{_projectNamespace}TaskModel task" : "")}
                    new CodeBlock("try",
                        new OneLineComment("ამოცანის შესრულება იწყება აქ"),
                        "",
                        "return true"),
                    new CodeBlock("catch (Exception e)",
                        "StShared.WriteException(e, true)",
                        "throw")
                )));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}