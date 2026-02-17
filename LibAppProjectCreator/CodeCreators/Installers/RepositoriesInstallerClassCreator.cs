//Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

using System;
using AppCliTools.CodeTools;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators.Installers;

public sealed class RepositoriesInstallerClassCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly string _projectShortName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RepositoriesInstallerClassCreator(ILogger logger, string placePath, string projectNamespace,
        string projectShortName, string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _projectShortName = projectShortName;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using Microsoft.AspNetCore.Builder", "using Microsoft.Extensions.DependencyInjection", "using System",
            "using System.Collections.Generic", string.Empty, "using CarcassDom", "using BackendCarcass.MasterData",
            "using CarcassRepositories", "using RepositoriesDom", "using WebInstallers", string.Empty,
            $"namespace {_projectNamespace}Repositories.Installers", string.Empty,
            new OneLineComment(" ReSharper disable once UnusedType.Global"),
            new CodeBlock("public sealed class RepositoriesInstaller : IInstaller", "public int InstallPriority => 30",
                "public int ServiceUsePriority => 30", string.Empty,
                new CodeBlock(
                    "public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args, Dictionary<string, string> parameters)",
                    new CodeBlock("if (debugMode)",
                        "Console.WriteLine($\"{GetType().Name}.{nameof(InstallServices)} Started\")"), string.Empty,
                    "builder.Services.AddScoped<IUserRightsRepository, UserRightsRepository>()",
                    $"builder.Services.AddScoped<IAbstractRepository, {_projectShortName.Capitalize()}AbstractRepository>()",
                    "builder.Services.AddScoped<IMasterDataLoaderCreator, MasterDataLoaderCreator>()",
                    "builder.Services.AddScoped<IReturnValuesLoaderCreator, ReturnValuesLoaderCreator>()",
                    new OneLineComment(
                        $"builder.Services.AddScoped<IMasterDataLoaderCreator, {_projectShortName.Capitalize()}MasterDataLoaderCrudCreator>()"),
                    new OneLineComment(
                        $"builder.Services.AddScoped<IReturnValuesLoaderCreator, {_projectShortName.Capitalize()}ReturnValuesLoaderCreator>()"),
                    string.Empty,
                    new CodeBlock("if (debugMode)",
                        "Console.WriteLine($\"{GetType().Name}.{nameof(InstallServices)} Started\")"), "return true"),
                string.Empty,
                new CodeBlock("public bool UseServices(WebApplication app, bool debugMode)", "return true")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}
