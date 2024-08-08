using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ApiProgramClassCreator : CodeCreator
{
    private readonly string _appKey;
    private readonly string _projectNamespace;
    private readonly bool _useReCounter;
    private readonly bool _useSignalR;
    private readonly bool _useFluentValidation;
    private readonly bool _useCarcass;
    private readonly bool _useDatabase;
    private readonly bool _useIdentity;
    private readonly bool _useReact;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiProgramClassCreator(ILogger logger, string placePath, string projectNamespace, string appKey,
        bool useDatabase, bool useReact, bool useCarcass, bool useIdentity, bool useReCounter, bool useSignalR,
        bool useFluentValidation,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _appKey = appKey;
        _useDatabase = useDatabase;
        _useReact = useReact;
        _useCarcass = useCarcass;
        _useIdentity = useIdentity;
        _useReCounter = useReCounter;
        _useSignalR = useSignalR;
        _useFluentValidation = useFluentValidation;
    }


    public override void CreateFileStructure()
    {
        var fluentValidationInstallerCodeCommands = _useFluentValidation
            ? new FlatCodeBlock(string.Empty,
                new OneLineComment("FluentValidationInstaller"),
                string.Empty,
                $"""
                 builder.Services.InstallValidation(
                 //BackendCarcass
                 BackendCarcassApi.AssemblyReference.Assembly
                 //{_projectNamespace}
                  )
                 """
            )
            : null;

        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using ConfigurationEncrypt",
            _useFluentValidation ? "using FluentValidationInstaller" : null,
            "using Microsoft.AspNetCore.Builder",
            "using Microsoft.Extensions.DependencyInjection",
            "using Serilog",
            _useSignalR ? "using SignalRMessages.Installers" : null,
            "using SwaggerTools",
            "using System",
            "using System.Collections.Generic",
            "using WebInstallers",
            "using Microsoft.Extensions.Hosting",
            string.Empty,
            new CodeBlock("try",
                $$"""
                  var parameters = new Dictionary<string, string>
                  {
                      {{(_useSignalR ? "{ SignalRMessagesInstaller.SignalRReCounterKey, string.Empty }, //Allow SignalRReCounterKey" : "")}}
                      { ConfigurationEncryptInstaller.AppKeyKey, "{{_appKey}}" },
                      { SwaggerInstaller.AppNameKey, "{{string.Join(" ", _projectNamespace.SplitUpperCase())}}" },
                      { SwaggerInstaller.VersionCountKey, 1.ToString() },
                      { SwaggerInstaller.UseSwaggerWithJwtBearerKey, string.Empty } //Allow Swagger
                  }
                  """,
                string.Empty,
                "var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory, Args = args })",
                string.Empty,
                "var debugMode = builder.Environment.IsDevelopment()",
                string.Empty,
                new CodeBlock($"""
                               if (!builder.InstallServices(debugMode, args, parameters, 

                               {(_useCarcass && _useDatabase ? $"""
                                                                //BackendCarcass
                                                                CarcassRepositories.AssemblyReference.Assembly,
                                                                BackendCarcassApi.AssemblyReference.Assembly,
                                                                CarcassDom.AssemblyReference.Assembly,
                                                                {(_useIdentity ? "CarcassIdentity.AssemblyReference.Assembly," : "")}
                                                                """ : null)}

                               //{_projectNamespace}DbPart
                               {_projectNamespace}Db.AssemblyReference.Assembly,

                               //WebSystemTools
                               ApiExceptionHandler.AssemblyReference.Assembly,
                               ConfigurationEncrypt.AssemblyReference.Assembly,
                               CorsTools.AssemblyReference.Assembly,
                               {(_useReCounter ? "ReCounterServiceInstaller.AssemblyReference.Assembly," : "")}
                               SerilogLogger.AssemblyReference.Assembly,
                               StaticFilesTools.AssemblyReference.Assembly,
                               SwaggerTools.AssemblyReference.Assembly,
                               TestToolsApi.AssemblyReference.Assembly,
                               WindowsServiceTools.AssemblyReference.Assembly
                               ))
                               """, "return 2"),
                string.Empty,
                $$$"""
                   builder.Services.AddMediatR(cfg =>
                   {{
                       //BackendCarcass
                       cfg.RegisterServicesFromAssembly(BackendCarcassApi.AssemblyReference.Assembly);
                       //{{{_projectNamespace}}}
                   }})
                   """,
                fluentValidationInstallerCodeCommands,
                string.Empty,
                new OneLineComment("ReSharper disable once using"),
                string.Empty,
                "using var app = builder.Build()",
                string.Empty,
                new CodeBlock("if (!app.UseServices(debugMode))", "return 1"),
                string.Empty,
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


/*
//{_projectNamespace}
{_projectNamespace}Repositories.AssemblyReference.Assembly,
{_projectNamespace}.AssemblyReference.Assembly
 */