using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class AppSettingsClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public AppSettingsClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "",
            $"namespace {_projectNamespace}.Models",
            "",
            new CodeBlock("public sealed class IdentitySettings",
                new CodeBlock("public string? JwtSecret", true, "get", "set")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}