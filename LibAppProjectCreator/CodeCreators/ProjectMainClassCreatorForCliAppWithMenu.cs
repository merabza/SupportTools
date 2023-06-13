using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectMainClassCreatorForCliAppWithMenu : CodeCreator
{
    private readonly string _projectNamespace;

    private readonly bool _useDatabase;
    //private readonly bool _taskWithParameters;

    public ProjectMainClassCreatorForCliAppWithMenu(ILogger logger, string placePath, string projectNamespace,
        bool useDatabase, string? codeFileName = null) : base(logger, placePath,
        codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
        //_taskWithParameters = taskWithParameters;
    }

    public override void CreateFileStructure()
    {
        var buildMainMenuBlock = new CodeBlock("protected override bool BuildMainMenu()",
            $"{_projectNamespace}Parameters parameters = ({_projectNamespace}Parameters)_parametersManager.Parameters",
            "",
            "CliMenuSet mainMenuSet = new (\"Main Menu\")",
            "AddChangeMenu(mainMenuSet)",
            "",
            new OneLineComment("ძირითადი პარამეტრების რედაქტირება"),
            $"{_projectNamespace}ParametersEditor {_projectNamespace.UnCapitalize()}ParametersEditor = new {_projectNamespace}ParametersEditor(parameters, _parametersManager, _logger)",
            $"mainMenuSet.AddMenuItem(new ParametersEditorListCommand({_projectNamespace.UnCapitalize()}ParametersEditor))",
            "",
            new OneLineComment("საჭირო მენიუს ელემენტები"),
            ""
        );

        FlatCodeBlock taskPart;
        //if (_taskWithParameters) // && ! _useDatabase
        //{
        //    //{(_useDatabase ? $", _{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric" : "")}
        //    taskPart = new FlatCodeBlock(new OneLineComment("ამოცანების განზოგადებული ვარიანტი"),
        //        $"TasksEditorMenu<ETools> {_projectNamespace.UnCapitalize()}TasksEditor = new TasksEditorMenu<ETools>(_logger, _parametersManager)",
        //        $"{_projectNamespace.UnCapitalize()}TasksEditor.TaskMenuElements(mainMenuSet)", "");
        //}
        //else
        //{
        taskPart = new FlatCodeBlock(
            "NewTaskCommand newAppTaskCommand = new(_parametersManager)",
            "mainMenuSet.AddMenuItem(newAppTaskCommand)",
            //,
            //$"mainMenuSet.AddMenuItem(new TaskCommand(_logger, _parametersManager, new {_projectNamespace}Runner(_logger{(_useDatabase ? $", _{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric" : "")}, parameters)), \"Run\")"
            new CodeBlock("foreach (KeyValuePair<string, TaskModel> kvp in parameters.Tasks.OrderBy(o => o.Key))",
                "mainMenuSet.AddMenuItem(new TaskSubMenuCommand(_logger, _parametersManager, kvp.Key))")
        );
        //}

        buildMainMenuBlock.AddRange(taskPart.CodeItems);

        var exitPart = new FlatCodeBlock(
            new OneLineComment("გასასვლელი"),
            "string key = ConsoleKey.Escape.Value().ToLower()",
            "mainMenuSet.AddMenuItem(key, \"Exit\", new ExitCommand(), key.Length)",
            "",
            "return true");
        buildMainMenuBlock.AddRange(exitPart.CodeItems);


        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliParameters",
            "using CliParameters.MenuCommands",
            "using CliMenu",
            "using LibDataInput",
            "using CliTools",
            "using CliTools.Commands",
            $"using {_projectNamespace}.MenuCommands",
            _useDatabase ? new CodeCommand($"using Do{_projectNamespace}.Models") : new CodeExtraLine(),
            "using Microsoft.Extensions.Logging",
            //"using SystemToolsShared",
            //_taskWithParameters ? new CodeCommand("using CliParameters.Tasks") : new CodeExtraLine(),
            $"using {_projectNamespace}.Models",
            //_useDatabase ? new CodeCommand($"using Do{_projectNamespace}") : new CodeExtraLine(),
            _useDatabase ? new CodeCommand($"using Lib{_projectNamespace}Repositories") : new CodeExtraLine(), "",
            $"namespace {_projectNamespace}",
            "",
            new CodeBlock($"public sealed class {_projectNamespace} : CliAppLoop",
                "private readonly ILogger _logger",
                "private readonly ParametersManager _parametersManager",
                _useDatabase
                    ? new CodeCommand(
                        $"private readonly I{_projectNamespace}RepositoryCreatorFabric _{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric")
                    : new CodeExtraLine(),
                "",
                new CodeBlock(
                    $"public {_projectNamespace}(ILogger logger, ParametersManager parametersManager{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFabric {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric" : "")})",
                    "_logger = logger",
                    "_parametersManager = parametersManager",
                    _useDatabase
                        ? new CodeCommand(
                            $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric = {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric")
                        : new CodeExtraLine()
                ),
                buildMainMenuBlock
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}