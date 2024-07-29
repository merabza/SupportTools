//Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

using System;
using System.Collections.Generic;
using CodeTools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace LibAppProjectCreator.CodeCreators.Installers;

public sealed class DatabaseInstallerClassCreator : CodeCreator
{
    private readonly JObject _appSettingsJsonJObject;
    private readonly List<string> _forEncodeAppSettingsJsonKeys;
    private readonly string _projectNamespace;
    private readonly JObject _userSecretJsonJObject;
    private readonly bool _useServerCarcass;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseInstallerClassCreator(ILogger logger, string placePath, string projectNamespace,
        JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys, bool useServerCarcass, string? codeFileName = null) :
        base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _appSettingsJsonJObject = appSettingsJsonJObject;
        _userSecretJsonJObject = userSecretJsonJObject;
        _forEncodeAppSettingsJsonKeys = forEncodeAppSettingsJsonKeys;
        _useServerCarcass = useServerCarcass;
    }

    public override void CreateFileStructure()
    {
        var databaseNameInParameters = _projectNamespace.Replace(".", string.Empty) + "Database";
        var connectionStringJsonKey = $"Data:{databaseNameInParameters}:ConnectionString";

        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            "using Microsoft.AspNetCore.Builder",
            "using Microsoft.EntityFrameworkCore",
            "using Microsoft.Extensions.DependencyInjection",
            "using System.Collections.Generic",
            "using WebInstallers",
            _useServerCarcass ? "using CarcassDb" : null,
            string.Empty,
            $"namespace {_projectNamespace}Db.Installers",
            string.Empty,
            new OneLineComment(" ReSharper disable once UnusedType.Global"),
            new CodeBlock($"public sealed class {_projectNamespace}DatabaseInstaller : IInstaller",
                "public int InstallPriority => 30",
                "public int ServiceUsePriority => 30",
                string.Empty,
                new CodeBlock(
                    "public void InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args, Dictionary<string, string> parameters)",
                    new CodeBlock("if (debugMode)",
                        "Console.WriteLine($\"{GetType().Name}.{nameof(InstallServices)} Started\")"),
                    "var connectionString = builder.Configuration[\"Data:AppGrammarGeDatabase:ConnectionString\"]",
                    string.Empty,
                    new CodeBlock("if (string.IsNullOrWhiteSpace(connectionString))",
                        $"Console.WriteLine(\"{_projectNamespace}DatabaseInstaller.InstallServices connectionString is empty\")",
                        "return"),
                    string.Empty,
                    _useServerCarcass
                        ? $"builder.Services.AddDbContext<CarcassDbContext>(options => options.UseSqlServer(builder.Configuration[\"{connectionStringJsonKey}\"]))"
                        : null,
                    $"builder.Services.AddDbContext<{_projectNamespace}DbContext>(options => options.UseSqlServer(builder.Configuration[\"{connectionStringJsonKey}\"]))",
                    new CodeBlock("if (debugMode)",
                        "Console.WriteLine($\"{GetType().Name}.{nameof(InstallServices)} Finished\")")),
                string.Empty,
                new CodeBlock("public void UseServices(WebApplication app, bool debugMode)")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();

        _appSettingsJsonJObject.Add(new JProperty("Data",
            new JObject(new JProperty(databaseNameInParameters,
                new JObject(new JProperty("ConnectionString", "DatabaseConnectionString"))))));
        _userSecretJsonJObject.Add(new JProperty("Data",
            new JObject(new JProperty(databaseNameInParameters,
                new JObject(new JProperty("ConnectionString",
                    $"Server=(local);Database={databaseNameInParameters}Development;Trusted_Connection=True;MultipleActiveResultSets=true;Application Name={_projectNamespace}"))))));

        _forEncodeAppSettingsJsonKeys.Add(connectionStringJsonKey);
    }
}