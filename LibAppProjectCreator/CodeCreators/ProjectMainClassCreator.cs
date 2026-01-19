using System;
using AppCliTools.CodeTools;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectMainClassCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    public ProjectMainClassCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var propertiesBlock = new FlatCodeBlock();

        if (_useDatabase)
        {
            propertiesBlock.Add(new CodeCommand($"using Do{_projectNamespace}.Models"));
            propertiesBlock.Add(new CodeCommand($"using Lib{_projectNamespace}Repositories"));
        }
        else
        {
            propertiesBlock.Add(new CodeCommand($"using {_projectNamespace}.Models"));
        }

        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliParameters", propertiesBlock, "using Microsoft.Extensions.Logging", string.Empty,
            $"namespace {_projectNamespace}", string.Empty, new CodeBlock(
                $"public sealed class {_projectNamespace} : ToolCommand", "private readonly ILogger _logger",
                _useDatabase
                    ? $"private readonly I{_projectNamespace}RepositoryCreatorFactory _{_projectNamespace.UnCapitalize()}RepositoryCreatorFactory"
                    : null,
                $"private {_projectNamespace}Parameters {_projectNamespace}Parameters => ({_projectNamespace}Parameters)Par",
                new CodeBlock(
                    $"public {_projectNamespace}(ILogger logger, ParametersManager parametersManager{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFactory {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory" : "")}) : base(logger, true, \"{_projectNamespace.SplitWithSpacesCamelParts()}\", \"{_projectNamespace.SplitWithSpacesCamelParts()}\", parametersManager)",
                    "_logger = logger",
                    _useDatabase
                        ? $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFactory = {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory"
                        : null),
                //new CodeBlock(
                //    $"public {_projectNamespace}(ILogger logger, ParametersTaskInfo parametersTaskInfo{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFactory {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory" : "")}) : base(logger, true, \"{_projectNamespace.SplitWithSpacesCamelParts()}\", null, parametersTaskInfo)",
                //    "_logger = logger",
                //    _useDatabase
                //        ? $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFactory = {_projectNamespace.UnCapitalize()}RepositoryCreatorFactory"
                //        : null),
                new CodeBlock("protected override bool RunAction()", "return true")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}