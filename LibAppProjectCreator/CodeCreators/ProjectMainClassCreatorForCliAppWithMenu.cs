using System;
using AppCliTools.CodeTools;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectMainClassCreatorForCliAppWithMenu : CodeCreator
{
    private readonly string _projectNamespace;

    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectMainClassCreatorForCliAppWithMenu(ILogger logger, string placePath, string projectNamespace,
        bool useDatabase, string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var buildMainMenuBlock = new CodeBlock("public override CliMenuSet BuildMainMenu()",
            $"var parameters = ({_projectNamespace}Parameters)_parametersManager.Parameters", string.Empty,
            "var mainMenuSet = new CliMenuSet(\"Main Menu\")",
            //"AddChangeMenu(mainMenuSet)",
            string.Empty, new OneLineComment("ძირითადი პარამეტრების რედაქტირება"),
            $"var {_projectNamespace.UnCapitalize()}ParametersEditor = new {_projectNamespace}ParametersEditor(parameters, _parametersManager, _logger{(_useDatabase ? ", _httpClientFactory" : "")})",
            $"mainMenuSet.AddMenuItem(new ParametersEditorListCliMenuCommand({_projectNamespace.UnCapitalize()}ParametersEditor))",
            string.Empty, new OneLineComment("საჭირო მენიუს ელემენტები"), string.Empty);

        var taskPart = new FlatCodeBlock("var newAppTaskCommand = new NewTaskCommand(_parametersManager)",
            "mainMenuSet.AddMenuItem(newAppTaskCommand)",
            new CodeBlock("foreach (var kvp in parameters.Tasks.OrderBy(o => o.Key))",
                "mainMenuSet.AddMenuItem(new TaskSubMenuCommand(_logger, _parametersManager, kvp.Key))"));

        buildMainMenuBlock.AddRange(taskPart.CodeItems);

        var exitPart = new FlatCodeBlock(new OneLineComment("პროგრამიდან გასასვლელი"),
            "var key = ConsoleKey.Escape.Value().ToLower()",
            "mainMenuSet.AddMenuItem(key, new ExitCliMenuCommand(), key.Length)", string.Empty, "return mainMenuSet");
        buildMainMenuBlock.AddRange(exitPart.CodeItems);

        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliMenu", "using CliParameters.CliMenuCommands", "using CliTools", "using CliTools.CliMenuCommands",
            "using LibDataInput", "using LibParameters", "using Microsoft.Extensions.Logging", "using System",
            "using System.Linq", _useDatabase ? "using System.Net.Http" : null,
            $"using {_projectNamespace}.MenuCommands",
            //$"using {_projectNamespace}.Models",
            _useDatabase ? new CodeCommand($"using Do{_projectNamespace}.Models") : new CodeExtraLine(), string.Empty,
            _useDatabase ? new CodeCommand($"using Lib{_projectNamespace}Repositories") : new CodeExtraLine(),
            string.Empty, $"namespace {_projectNamespace}", string.Empty,
            new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
            new CodeBlock($"public sealed class {_projectNamespace} : CliAppLoop", "private readonly ILogger _logger",
                _useDatabase ? "private readonly IHttpClientFactory _httpClientFactory" : null,
                "private readonly ParametersManager _parametersManager",
                _useDatabase
                    ? new OneLineComment(
                        "ეს საჭიროა იმ შემთხვევაში, თუ ბაზაში რედაქტორები გვინდა გავაკეთოთ და საჭიროა, რომ ამ მენიუდან მოხდეს გამოძახება")
                    : new CodeExtraLine(),
                _useDatabase
                    ? new CodeCommand(
                        $"private readonly I{_projectNamespace}RepositoryCreatorFactory _{_projectNamespace.UnCapitalize()}RepositoryCreatorFactory")
                    : new CodeExtraLine(), string.Empty,
                new CodeBlock(
                    $"public {_projectNamespace}(ILogger logger{(_useDatabase ? ", IHttpClientFactory httpClientFactory" : string.Empty)}, ParametersManager parametersManager{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFactory {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory" : string.Empty)})",
                    "_logger = logger", _useDatabase ? "_httpClientFactory = httpClientFactory" : null,
                    "_parametersManager = parametersManager",
                    _useDatabase
                        ? new CodeCommand(
                            $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFactory = {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory")
                        : new CodeExtraLine()), buildMainMenuBlock));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}