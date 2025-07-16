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

    // ReSharper disable once ConvertToPrimaryConstructor
    public ConsoleProgramClassCreator(ILogger logger, string placePath, string projectNamespace, string? appKey,
        bool useDatabase, string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _appKey = appKey;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var parametersClassName = $"{_projectNamespace}Parameters";
        var projectLow = _projectNamespace.UnCapitalize();

        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System", _useDatabase ? "using System.Net.Http" : null, "using SystemToolsShared",
            "using CliParameters", "using LibParameters", $"using {_projectNamespace}",
            _useDatabase ? $"using Lib{_projectNamespace}Repositories" : null,
            $"using {(_useDatabase ? "Do" : string.Empty)}{_projectNamespace}.Models",
            "using Microsoft.Extensions.DependencyInjection", "using Microsoft.Extensions.Logging", "using Serilog",
            "using Serilog.Events", string.Empty, "ILogger<Program>? logger = null", new CodeBlock("try",
                "Console.WriteLine(\"Loading...\")", string.Empty, $"const string appName = \"{_projectNamespace}\"",
                string.Empty, new OneLineComment("პროგრამის ატრიბუტების დაყენება "),
                "ProgramAttributes.Instance.AppName = appName",
                _appKey is null ? null : $"ProgramAttributes.Instance.SetAttribute(\"AppKey\", \"{_appKey}\")",
                string.Empty,
                //თუ მენიუს გამოყენება არ ხდება, მაშინ დაშიფვრას აზრი არ აქვს, რადგან ასეთ შემთხვევაში საჭირო იქნება ცალკე რედაქტორის და დამშიფრავის არსებობა
                $"var argParser = new ArgumentsParser<{parametersClassName}>(args, appName, null)",
                new CodeBlock("switch (argParser.Analysis())", "case EParseResult.Ok: break",
                    "case EParseResult.Usage: return 1", "case EParseResult.Error: return 2",
                    "default: throw new ArgumentOutOfRangeException()"), string.Empty,
                $"var par = ({parametersClassName}?)argParser.Par",
                new CodeBlock("if (par is null)", "StShared.WriteErrorLine(\"ConsoleTestParameters is null\", true)",
                    "return 3"), string.Empty, "var parametersFileName = argParser.ParametersFileName",
                $"var servicesCreator = new {(_useDatabase ? _projectNamespace : string.Empty)}ServicesCreator({(_useDatabase ? "par" : $"par.LogFolder, null, \"{_projectNamespace}\"")})",
                new OneLineComment(" ReSharper disable once using"),
                "var serviceProvider = servicesCreator.CreateServiceProvider(LogEventLevel.Information)", string.Empty,
                new CodeBlock("if (serviceProvider == null)", "StShared.WriteErrorLine(\"Logger not created\", true)",
                    "return 4"), string.Empty, "logger = serviceProvider.GetService<ILogger<Program>>()",
                new CodeBlock("if (logger is null)", "StShared.WriteErrorLine(\"logger is null\", true)", "return 5"),
                string.Empty,
                _useDatabase
                    ? new FlatCodeBlock(
                        new CodeCommand(
                            $"var {projectLow}RepositoryCreatorFactory = serviceProvider.GetService<I{_projectNamespace}RepositoryCreatorFactory>()"),
                        new CodeBlock($"if ({projectLow}RepositoryCreatorFactory is null)",
                            $"StShared.WriteErrorLine(\"{projectLow}RepositoryCreatorFactory is null\", true)",
                            "return 6"), string.Empty,
                        new CodeCommand("var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>()"),
                        new CodeBlock("if (httpClientFactory is null)",
                            "StShared.WriteErrorLine(\"httpClientFactory is null\", true)", "return 6"), string.Empty)
                    : null, string.Empty,
                $"var {projectLow} = new {_projectNamespace}.{_projectNamespace}(logger{(_useDatabase ? ", httpClientFactory" : string.Empty)}, new ParametersManager(parametersFileName, par){(_useDatabase ? $", {projectLow}RepositoryCreatorFactory" : string.Empty)})",
                string.Empty, $"return {projectLow}.Run() ? 0 : 1"),
            new CodeBlock("catch (Exception e)", "StShared.WriteException(e, true, logger)", "return 7"),
            new CodeBlock("finally", "Log.CloseAndFlush()"));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}