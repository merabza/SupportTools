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
            propertiesBlock.Add(new FlatCodeBlock("using DbTools"));
        }


        var fieldEditorsBlock = new FlatCodeBlock();

        if (_useDatabase)
        {
            fieldEditorsBlock.Add(new FlatCodeBlock(
                $"FieldEditors.Add(new EnumFieldEditor<EDataProvider>(nameof({_projectNamespace}Parameters.DataProvider), EDataProvider.Sql))"));
            fieldEditorsBlock.Add(new FlatCodeBlock(
                $"FieldEditors.Add(new ConnectionStringFieldEditor(logger, nameof({_projectNamespace}Parameters.ConnectionString), nameof({_projectNamespace}Parameters.DataProvider), parametersManager))"));
            fieldEditorsBlock.Add(new FlatCodeBlock(
                $"FieldEditors.Add(new IntFieldEditor(nameof({_projectNamespace}Parameters.CommandTimeOut), 10000))"));
        }


        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using LibParameters",
            "using CliParameters.FieldEditors",
            propertiesBlock,
            "using Microsoft.Extensions.Logging",
            "",
            $"namespace {_projectNamespace}.Models",
            "",
            new CodeBlock($"public sealed class {_projectNamespace}ParametersEditor : ParametersEditor",
                new CodeBlock(
                    $"public {_projectNamespace}ParametersEditor(IParameters parameters, IParametersManager parametersManager, ILogger logger) : base(\"{_projectNamespace} Parameters Editor\", parameters, parametersManager)",
                    $"FieldEditors.Add(new FolderPathFieldEditor(nameof({_projectNamespace}Parameters.LogFolder)))",
                    fieldEditorsBlock
                )
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}