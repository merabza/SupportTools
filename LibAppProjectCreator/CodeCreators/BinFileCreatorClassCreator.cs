using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class BinFileCreatorClassCreator : CodeCreator
{
    private readonly string _base64String;
    private readonly string _creatorClassName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BinFileCreatorClassCreator(ILogger logger, string placePath, string creatorClassName, string base64String,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _creatorClassName = creatorClassName;
        _base64String = base64String;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CodeTools", "using Microsoft.Extensions.Logging", string.Empty,
            "namespace LibAppProjectCreator.CodeCreators.React", string.Empty,
            new CodeBlock($"public sealed class {_creatorClassName} : BinFileCreator",
                new CodeBlock(
                    $"public {_creatorClassName}(ILogger logger, string placePath, string binFileName) : base(logger, placePath, binFileName)",
                    string.Empty), string.Empty,
                new CodeBlock("public override void CreateFileData()", $"BinFile.Base64String = @\"{_base64String}\"",
                    "FinishAndSave()"), string.Empty));


        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}