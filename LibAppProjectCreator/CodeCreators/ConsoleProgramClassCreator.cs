using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ConsoleProgramClassCreator : CodeCreator
{
    private readonly string? _appKey;
    private readonly string _projectNamespace;

    private readonly bool _useDatabase;

    private readonly bool _useMenu;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ConsoleProgramClassCreator(ILogger logger, string placePath, string projectNamespace, string? appKey,
        bool useDatabase, bool useMenu, string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _appKey = appKey;
        _useDatabase = useDatabase;
        _useMenu = useMenu;
    }


    public override void CreateFileStructure()
    {
        var parametersClassName = $"{_projectNamespace}Parameters";
        var projectLow = _projectNamespace.UnCapitalize();

        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            "using SystemToolsShared",
            "using CliParameters",
            "using LibParameters",
            _useDatabase ? $"using Lib{_projectNamespace}Repositories" : null,
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using Microsoft.Extensions.DependencyInjection",
            "using Microsoft.Extensions.Logging",
            "using Serilog",
            "using Serilog.Events",
            "",
            "ILogger<Program>? logger = null",
            new CodeBlock("try",
                "Console.WriteLine(\"Loading...\")",
                "",
                new OneLineComment("პროგრამის ატრიბუტების დაყენება "),
                $"ProgramAttributes.Instance.SetAttribute(\"AppName\", \"{string.Join(" ", _projectNamespace.SplitUpperCase())}\")",
                _appKey is null ? null : $"ProgramAttributes.Instance.SetAttribute(\"AppKey\", \"{_appKey}\")",
                "",
                //თუ მენიუს გამოყენება არ ხდება, მაშინ დაშიფვრას აზრი არ აქვს, რადგან ასეთ შემთხვევაში საჭირო იქნება ცალკე რედაქტორის და დამშიფრავის არსებობა
                $"var argParser = new ArgumentsParser<{parametersClassName}>(args, \"{_projectNamespace}\", {(_useMenu ? "ProgramAttributes.Instance.GetAttribute<string>(\"AppKey\") + Environment.MachineName.Capitalize()" : "null")})",
                new CodeBlock("switch (argParser.Analysis())",
                    "case EParseResult.Ok: break",
                    "case EParseResult.Usage: return 1",
                    "case EParseResult.Error: return 2",
                    "default: throw new ArgumentOutOfRangeException()"),
                "",
                $"var par = ({parametersClassName}?)argParser.Par",
                new CodeBlock("if (par is null)",
                    "StShared.WriteErrorLine(\"ConsoleTestParameters is null\", true)",
                    "return 3"),
                "",
                "var parametersFileName = argParser.ParametersFileName",
                $"var servicesCreator = new {(_useDatabase ? _projectNamespace : "")}ServicesCreator({(_useDatabase ? "par" : $"par.LogFolder, null, \"{_projectNamespace}\"")})",
                new OneLineComment(" ReSharper disable once using"),
                "var serviceProvider = servicesCreator.CreateServiceProvider(LogEventLevel.Information)",
                "",
                new CodeBlock("if (serviceProvider == null)",
                    "StShared.WriteErrorLine(\"Logger not created\", true)",
                    "return 4"),
                "",
                "logger = serviceProvider.GetService<ILogger<Program>>()",
                new CodeBlock("if (logger is null)",
                    "StShared.WriteErrorLine(\"logger is null\", true)",
                    "return 5"),
                "",
                _useDatabase
                    ? new FlatCodeBlock(
                        new CodeCommand(
                            $"var {projectLow}RepositoryCreatorFabric = serviceProvider.GetService<I{_projectNamespace}RepositoryCreatorFabric>()"),
                        new CodeBlock($"if ({projectLow}RepositoryCreatorFabric is null)",
                            $"StShared.WriteErrorLine(\"{projectLow}RepositoryCreatorFabric is null\", true)",
                            "return 6"),
                        ""
                    )
                    : null,
                "",
                $"var {projectLow} = new {_projectNamespace}.{_projectNamespace}(logger, new ParametersManager(parametersFileName, par){(_useDatabase ? $", {projectLow}RepositoryCreatorFabric" : "")})",
                "",
                $"return {projectLow}.Run() ? 0 : 1"),
            new CodeBlock("catch (Exception e)",
                "StShared.WriteException(e, true, logger)",
                "return 7"),
            new CodeBlock("finally",
                "Log.CloseAndFlush()"));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}