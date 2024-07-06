using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectParametersEditorClassCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectParametersEditorClassCreator(ILogger logger, string placePath, string projectNamespace,
        bool useDatabase, string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }


    public override void CreateFileStructure()
    {
        var propertiesBlock = new FlatCodeBlock();

        if (_useDatabase)
        {
            propertiesBlock.Add(new FlatCodeBlock($"using Do{_projectNamespace}.Models"));
            propertiesBlock.Add(new FlatCodeBlock("using CliParametersDataEdit.FieldEditors"));
        }


        var fieldEditorsBlock = new FlatCodeBlock();

        if (_useDatabase)
            fieldEditorsBlock.Add(new FlatCodeBlock(
                $"FieldEditors.Add(new DatabaseConnectionParametersFieldEditor(logger, nameof({_projectNamespace}Parameters.DatabaseConnectionParameters), parametersManager))"));


        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliParameters",
            "using CliParameters.FieldEditors",
            "using LibParameters",
            "using Microsoft.Extensions.Logging",
            $"using {_projectNamespace}.Models",
            propertiesBlock,
            string.Empty,
            $"namespace {_projectNamespace}.Models",
            string.Empty,
            new CodeBlock($"public sealed class {_projectNamespace}ParametersEditor : ParametersEditor",
                new CodeBlock(
                    $"public {_projectNamespace}ParametersEditor(IParameters parameters, ParametersManager parametersManager, ILogger logger) : base(\"{_projectNamespace} Parameters Editor\", parameters, parametersManager)",
                    $"FieldEditors.Add(new FolderPathFieldEditor(nameof({_projectNamespace}Parameters.LogFolder)))",
                    fieldEditorsBlock
                )
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}