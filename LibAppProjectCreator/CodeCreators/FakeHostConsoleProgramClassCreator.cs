using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class FakeHostConsoleProgramClassCreator : CodeCreator
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public FakeHostConsoleProgramClassCreator(ILogger logger, string placePath, string? codeFileName = null) : base(
        logger,
        placePath, codeFileName)
    {
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "",
            "using Microsoft.AspNetCore.Builder",
            "",
            "var builder = WebApplication.CreateBuilder(args)",
            "",
            new OneLineComment("Add services to the container."),
            "",
            "var app = builder.Build()",
            "",
            new OneLineComment("Configure the HTTP request pipeline."),
            "",
            "app.Run()");
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}