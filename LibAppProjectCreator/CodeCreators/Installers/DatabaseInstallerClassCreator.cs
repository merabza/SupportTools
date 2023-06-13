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
        var databaseNameInParameters = _projectNamespace.Replace(".", "") + "Database";
        var connectionStringJsonKey = $"Data:{databaseNameInParameters}:ConnectionString";

        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            _useServerCarcass ? "using CarcassDb" : null,
            //$"using {_projectNamespace}Db",
            "using Microsoft.AspNetCore.Builder",
            "using Microsoft.EntityFrameworkCore",
            "using Microsoft.Extensions.DependencyInjection",
            "using WebInstallers",
            "",
            $"namespace {_projectNamespace}Db.Installers",
            "",
            new OneLineComment(" ReSharper disable once UnusedType.Global"),
            new CodeBlock($"public sealed class {_projectNamespace}DatabaseInstaller : IInstaller",
                "public int InstallPriority => 30",
                "public int ServiceUsePriority => 30",
                "",
                new CodeBlock("public void InstallServices(WebApplicationBuilder builder, string[] args)",
                    "Console.WriteLine(\"DatabaseInstaller.InstallServices Started\")",
                    "",
                    _useServerCarcass
                        ? $"builder.Services.AddDbContext<CarcassDbContext>(options => options.UseSqlServer(builder.Configuration[\"{connectionStringJsonKey}\"]))"
                        : null,
                    $"builder.Services.AddDbContext<{_projectNamespace}DbContext>(options => options.UseSqlServer(builder.Configuration[\"{connectionStringJsonKey}\"]))",
                    "Console.WriteLine(\"DatabaseInstaller.InstallServices Finished\")"),
                "",
                new CodeBlock("public void UseServices(WebApplication app)")));
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