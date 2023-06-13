using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectServicesCreatorClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public ProjectServicesCreatorClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            //"using CliShared",
            $"using {_projectNamespace}Db",
            $"using Do{_projectNamespace}.Models",
            $"using Lib{_projectNamespace}Repositories",
            "using Microsoft.EntityFrameworkCore",
            "using Microsoft.Extensions.DependencyInjection",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}",
            "",
            new CodeBlock($"public sealed class {_projectNamespace}ServicesCreator : ServicesCreator",
                $"private readonly {_projectNamespace}Parameters _par",
                new CodeBlock(
                    $"public {_projectNamespace}ServicesCreator({_projectNamespace}Parameters par) : base(par.LogFolder, null, \"{_projectNamespace}\")",
                    "_par = par"
                ),
                new CodeBlock("protected override void ConfigureServices(IServiceCollection services)",
                    "base.ConfigureServices(services)",
                    "",
                    new CodeBlock("if (!string.IsNullOrEmpty(_par.ConnectionString) )",
                        $"services.AddDbContext<{_projectNamespace}DbContext>(options => options.UseSqlServer(_par.ConnectionString))"),
                    $"services.AddScoped<I{_projectNamespace}Repository, {_projectNamespace}Repository>()",
                    $"services.AddSingleton<I{_projectNamespace}RepositoryCreatorFabric, {_projectNamespace}RepositoryCreatorFabric>()"
                )));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}