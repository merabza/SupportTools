using System;
using AppCliTools.CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class FakeHostConsoleProgramClassCreator : CodeCreator
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public FakeHostConsoleProgramClassCreator(ILogger logger, string placePath, string? codeFileName = null) : base(
        logger, placePath, codeFileName)
    {
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, "using Microsoft.AspNetCore.Builder", string.Empty,
            "var builder = WebApplication.CreateBuilder(args)", string.Empty,
            new OneLineComment("Add services to the container."), string.Empty,
            new OneLineComment(" ReSharper disable once using"), "var app = builder.Build()", string.Empty,
            new OneLineComment("Configure the HTTP request pipeline."), string.Empty, "app.Run()");
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}
