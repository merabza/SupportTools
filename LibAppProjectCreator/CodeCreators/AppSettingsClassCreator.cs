using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class AppSettingsClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppSettingsClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, $"namespace {_projectNamespace}.Models", string.Empty,
            new CodeBlock("public sealed class IdentitySettings",
                new CodeBlock("public string? JwtSecret", true, "get", "set")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}