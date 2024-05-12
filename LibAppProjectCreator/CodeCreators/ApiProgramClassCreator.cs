using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ApiProgramClassCreator : CodeCreator
{
    private readonly string _appKey;
    private readonly string _projectNamespace;
    private readonly bool _useBackgroundTasks;
    private readonly bool _useCarcass;
    private readonly bool _useDatabase;
    private readonly bool _useIdentity;
    private readonly bool _useReact;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiProgramClassCreator(ILogger logger, string placePath, string projectNamespace, string appKey,
        bool useDatabase, bool useReact, bool useCarcass, bool useIdentity, bool useBackgroundTasks,
        string? codeFileName = null) : base(logger, placePath,
        codeFileName)
    {
        _projectNamespace = projectNamespace;
        _appKey = appKey;
        _useDatabase = useDatabase;
        _useReact = useReact;
        _useCarcass = useCarcass;
        _useIdentity = useIdentity;
        _useBackgroundTasks = useBackgroundTasks;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            new OneLineComment("using System.IO"),
            "using ConfigurationEncrypt",
            "using Microsoft.AspNetCore.Builder",
            "using Serilog",
            "using SerilogLogger",
            "using SwaggerTools",
            "using SystemToolsShared",
            "using WebInstallers",
            "using TestToolsApi.Endpoints.V1",
            "using WindowsServiceTools",
            _useDatabase ? $"using {_projectNamespace}Db.Installers" : null,
            _useReact ? "using ReactTools" : null,
            _useCarcass ? "using CarcassRepositories.Installers" : null,
            _useCarcass ? $"using {_projectNamespace}MasterDataLoaders.Installers" : null,
            _useIdentity && _useCarcass ? "using CarcassIdentity.Installers" : null,
            _useBackgroundTasks ? "using BackgroundTasksTools" : null,
            _useCarcass ? "using ServerCarcassMini.Endpoints.V1" : null,
            //$"using {_projectNamespace}",
            //$"using {_projectNamespace}.Installers",
            "",
            new CodeBlock("try",
                new OneLineComment("პროგრამის ატრიბუტების დაყენება "),
                $"ProgramAttributes.Instance.SetAttribute(\"AppName\", \"{string.Join(" ", _projectNamespace.SplitUpperCase())}\")",
                $"ProgramAttributes.Instance.SetAttribute(\"AppKey\", \"{_appKey}\")",
                "ProgramAttributes.Instance.SetAttribute(\"VersionCount\", 1)",
                $"ProgramAttributes.Instance.SetAttribute(\"UseSwaggerWithJWTBearer\", {_useIdentity.ToString().ToLower()})",
                "",
                "var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory, Args = args })",
                "",
                $"builder.InstallServices(args, typeof(TestEndpoints), typeof(ConfigurationEncryptInstaller), typeof(SerilogLoggerInstaller), typeof(UseWindowsServiceInstaller), typeof(SwaggerInstaller){(_useDatabase ? $", typeof({_projectNamespace}DatabaseInstaller)" : "")}{(_useReact ? ", typeof(ReactInstaller)" : "")}{(_useCarcass ? ", typeof(CarcassRepositoriesInstaller)" : "")}{(_useDatabase && _useCarcass ? ", typeof(RepositoriesInstaller)" : "")}{(_useCarcass && _useIdentity ? ", typeof(CarcassIdentityInstaller)" : "")}{(_useBackgroundTasks ? ", typeof(BackgroundTasksQueueInstaller)" : "")}{(_useCarcass ? ", typeof(DataTypesEndpoints)" : "")})",
                "",
                "var app = builder.Build()",
                "",
                "app.UseServices()",
                "",
                new OneLineComment(
                    "Log.Information(\"Directory.GetCurrentDirectory() = {0}\", Directory.GetCurrentDirectory())"),
                new OneLineComment("Log.Information(\"AppContext.BaseDirectory = {0}\", AppContext.BaseDirectory)"),
                "",
                "app.Run()",
                "return 0"),
            new CodeBlock("catch (Exception e)",
                "Log.Fatal(e, \"Host terminated unexpectedly\")",
                "return 1"),
            new CodeBlock("finally",
                "Log.CloseAndFlush()"));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}