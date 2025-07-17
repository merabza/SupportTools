using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class EToolsEnumCreator : CodeCreator
{
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EToolsEnumCreator(ILogger logger, string placePath, string projectName, string? codeFileName = null) : base(
        logger, placePath, codeFileName)
    {
        _projectName = projectName;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, $"namespace {_projectName}", string.Empty, new CodeEnum("public enum ETools", _projectName));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}