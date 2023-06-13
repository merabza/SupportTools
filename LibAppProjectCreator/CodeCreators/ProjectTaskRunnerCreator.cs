using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectTaskRunnerCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    public ProjectTaskRunnerCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger,
        placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using Microsoft.Extensions.Logging",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}",
            "",
            new CodeBlock($"public sealed class {_projectNamespace}TaskRunner",
                "private readonly ILogger _logger",
                $"private readonly {_projectNamespace}Parameters _par",
                "private readonly string? _taskName",
                "private readonly TaskModel? _task",
                "",
                new CodeBlock(
                    $"public {_projectNamespace}TaskRunner(ILogger logger, {_projectNamespace}Parameters par, string taskName, TaskModel task)",
                    "_logger = logger",
                    "_par = par",
                    "_taskName = taskName",
                    "_task = task"),
                new CodeBlock(
                    $"public {_projectNamespace}TaskRunner(ILogger logger, {_projectNamespace}Parameters par)",
                    "_logger = logger",
                    "_par = par",
                    "_taskName = null",
                    "_task = null"),
                new CodeBlock("public void Run()",
                    new CodeBlock("try",
                        ""),
                    new CodeBlock("catch (Exception e)",
                        "StShared.WriteException(e, true)",
                        "throw"))
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}