using System;
using AppCliTools.CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class DeleteTaskCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteTaskCommandCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System", "using CliMenu", "using LibParameters",
            $"using {(_useDatabase ? "Do" : string.Empty)}{_projectNamespace}.Models", "using LibDataInput",
            "using SystemTools.SystemToolsShared", string.Empty, $"namespace {_projectNamespace}.MenuCommands", string.Empty,
            new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
            new CodeBlock("public sealed class DeleteTaskCommand : CliMenuCommand",
                "private readonly ParametersManager _parametersManager", "private readonly string _taskName",
                new CodeBlock(
                    "public DeleteTaskCommand(ParametersManager parametersManager, string taskName) : base(\"Delete Task\", EMenuAction.LevelUp)",
                    "_parametersManager = parametersManager", "_taskName = taskName"),
                new CodeBlock("protected override bool RunBody()",
                    $"var parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                    "var task = parameters.GetTask(_taskName)",
                    new CodeBlock("if (task == null)",
                        "StShared.WriteErrorLine($\"Task { _taskName } does not found\", true)", "return false"),
                    new CodeBlock(
                        "if (!Inputer.InputBool($\"This will Delete  Task { _taskName }.are you sure ? \", false, false))",
                        "return false"), "parameters.RemoveTask(_taskName)",
                    "_parametersManager.Save(parameters, $\"Task { _taskName } deleted.\")", "return true")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}