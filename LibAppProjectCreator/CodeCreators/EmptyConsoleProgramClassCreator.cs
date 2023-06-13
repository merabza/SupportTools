using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class EmptyConsoleProgramClassCreator : CodeCreator
{
    public EmptyConsoleProgramClassCreator(ILogger logger, string placePath, string? codeFileName = null) : base(logger,
        placePath, codeFileName)
    {
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            "",
            "Console.WriteLine(\"Hello, World!\")");
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}