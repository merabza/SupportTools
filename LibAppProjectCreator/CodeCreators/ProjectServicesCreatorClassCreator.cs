﻿using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectServicesCreatorClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectServicesCreatorClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            //"using CliShared",
            $"using {_projectNamespace}Db", $"using Do{_projectNamespace}.Models",
            $"using Lib{_projectNamespace}Repositories", "using Microsoft.EntityFrameworkCore",
            "using Microsoft.Extensions.DependencyInjection", "using SystemToolsShared", string.Empty,
            $"namespace {_projectNamespace}", string.Empty,
            new CodeBlock($"public sealed class {_projectNamespace}ServicesCreator : ServicesCreator",
                $"private readonly {_projectNamespace}Parameters _par",
                new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
                new CodeBlock(
                    $"public {_projectNamespace}ServicesCreator({_projectNamespace}Parameters par) : base(par.LogFolder, null, \"{_projectNamespace}\")",
                    "_par = par"),
                new CodeBlock("protected override void ConfigureServices(IServiceCollection services)",
                    "base.ConfigureServices(services)", string.Empty,
                    new CodeBlock("if (!string.IsNullOrEmpty(_par.DatabaseConnectionParameters?.ConnectionString))",
                        $"services.AddDbContext<{_projectNamespace}DbContext>(options => options.UseSqlServer(_par.DatabaseConnectionParameters.ConnectionString))"),
                    $"services.AddScoped<I{_projectNamespace}Repository, {_projectNamespace}Repository>()",
                    $"services.AddSingleton<I{_projectNamespace}RepositoryCreatorFactory, {_projectNamespace}RepositoryCreatorFactory>()")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}