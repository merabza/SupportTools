﻿using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectMainClassCreatorForCliAppWithMenu : CodeCreator
{
    private readonly string _projectNamespace;

    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectMainClassCreatorForCliAppWithMenu(ILogger logger, string placePath, string projectNamespace,
        bool useDatabase, string? codeFileName = null) : base(logger, placePath,
        codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var buildMainMenuBlock = new CodeBlock("protected override bool BuildMainMenu()",
            $"var parameters = ({_projectNamespace}Parameters)_parametersManager.Parameters",
            "",
            "var mainMenuSet = new CliMenuSet(\"Main Menu\")",
            "AddChangeMenu(mainMenuSet)",
            "",
            new OneLineComment("ძირითადი პარამეტრების რედაქტირება"),
            $"var {_projectNamespace.UnCapitalize()}ParametersEditor = new {_projectNamespace}ParametersEditor(parameters, _parametersManager, _logger)",
            $"mainMenuSet.AddMenuItem(new ParametersEditorListCliMenuCommand({_projectNamespace.UnCapitalize()}ParametersEditor))",
            "",
            new OneLineComment("საჭირო მენიუს ელემენტები"),
            ""
        );

        var taskPart = new FlatCodeBlock(
            "NewTaskCommand newAppTaskCommand = new(_parametersManager)",
            "mainMenuSet.AddMenuItem(newAppTaskCommand)",
            new CodeBlock("foreach (var kvp in parameters.Tasks.OrderBy(o => o.Key))",
                "mainMenuSet.AddMenuItem(new TaskSubMenuCommand(_logger, _parametersManager, kvp.Key))")
        );

        buildMainMenuBlock.AddRange(taskPart.CodeItems);

        var exitPart = new FlatCodeBlock(
            new OneLineComment("გასასვლელი"),
            "var key = ConsoleKey.Escape.Value().ToLower()",
            "mainMenuSet.AddMenuItem(key, \"Exit\", new ExitCliMenuCommand(), key.Length)",
            "",
            "return true");
        buildMainMenuBlock.AddRange(exitPart.CodeItems);


        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliMenu",
            "using CliParameters.CliMenuCommands",
            "using CliTools",
            "using CliTools.CliMenuCommands",
            "using LibDataInput",
            "using LibParameters",
            "using Microsoft.Extensions.Logging",
            "using System",
            "using System.Linq",
            $"using {_projectNamespace}.MenuCommands",
            $"using {_projectNamespace}.Models",
            _useDatabase ? new CodeCommand($"using Do{_projectNamespace}.Models") : new CodeExtraLine(), "",
            _useDatabase ? new CodeCommand($"using Lib{_projectNamespace}Repositories") : new CodeExtraLine(), "",
            $"namespace {_projectNamespace}",
            "",
            new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
            new CodeBlock($"public sealed class {_projectNamespace} : CliAppLoop",
                "private readonly ILogger _logger",
                "private readonly ParametersManager _parametersManager",
                _useDatabase
                    ? new OneLineComment(
                        "ეს საჭიროა იმ შემთხვევაში, თუ ბაზაში რედაქტორები გვინდა გავაკეთოთ და საჭიროა, რომ ამ მენიუდან მოხდეს გამოძახება")
                    : new CodeExtraLine(),
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