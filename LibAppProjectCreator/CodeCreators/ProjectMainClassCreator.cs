using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
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

        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliParameters",
            propertiesBlock,
            "using Microsoft.Extensions.Logging",
            "",
            $"namespace {_projectNamespace}",
            "",
            new CodeBlock($"public sealed class {_projectNamespace} : ToolCommand",
                "private readonly ILogger _logger",
                _useDatabase
                    ? $"private readonly I{_projectNamespace}RepositoryCreatorFabric _{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric"
                    : null,
                $"private {_projectNamespace}Parameters {_projectNamespace}Parameters => ({_projectNamespace}Parameters)Par",
                new CodeBlock(
                    $"public {_projectNamespace}(ILogger logger, ParametersManager parametersManager{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFabric {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric" : "")}) : base(logger, true, \"{_projectNamespace.SplitWithSpacesCamelParts()}\", \"{_projectNamespace.SplitWithSpacesCamelParts()}\", parametersManager)",
                    "_logger = logger",
                    _useDatabase
                        ? $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric = {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric"
                        : null),
                //new CodeBlock(
                //    $"public {_projectNamespace}(ILogger logger, ParametersTaskInfo parametersTaskInfo{(_useDatabase ? $", I{_projectNamespace}RepositoryCreatorFabric {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric" : "")}) : base(logger, true, \"{_projectNamespace.SplitWithSpacesCamelParts()}\", null, parametersTaskInfo)",
                //    "_logger = logger",
                //    _useDatabase
                //        ? $"_{_projectNamespace.UnCapitalize()}RepositoryCreatorFabric = {_projectNamespace.UnCapitalize()}RepositoryCreatorFabric"
                //        : null),
                new CodeBlock("protected override bool RunAction()",
                    "return true"
                )));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}