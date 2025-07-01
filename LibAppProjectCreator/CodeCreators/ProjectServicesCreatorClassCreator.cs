using System;
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
            "using System",
            "using CliParametersDataEdit",
            "using LibDatabaseParameters",
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
                    "DatabaseServerConnections databaseServerConnections = new(_par.DatabaseServerConnections)",
                    string.Empty,
                    "var (dataProvider, connectionString) = DbConnectionFactory.GetDataProviderAndConnectionString(_par.DatabaseParameters, databaseServerConnections)",
                    string.Empty,
                    new CodeBlock("if (!string.IsNullOrEmpty(connectionString))",
                        new CodeBlock("switch (dataProvider)",
                            $"case EDatabaseProvider.SqlServer: services.AddDbContext<{_projectNamespace}DbContext>(options => options.UseSqlServer(connectionString))",
                            "break",
                            "case EDatabaseProvider.None: case EDatabaseProvider.SqLite: case EDatabaseProvider.OleDb: case EDatabaseProvider.WebAgent: case null: break",
                            "default: throw new ArgumentOutOfRangeException(nameof(dataProvider))")),
                    $"services.AddScoped<I{_projectNamespace}Repository, {_projectNamespace}Repository>()",
                    $"services.AddSingleton<I{_projectNamespace}RepositoryCreatorFactory, {_projectNamespace}RepositoryCreatorFactory>()",
                    "services.AddHttpClient()")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}