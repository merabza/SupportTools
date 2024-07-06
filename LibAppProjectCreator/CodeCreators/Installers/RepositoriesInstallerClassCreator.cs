//Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

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
        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            "using CarcassRepositories",
            "using Microsoft.AspNetCore.Builder",
            "using Microsoft.Extensions.DependencyInjection",
            "using WebInstallers",
            string.Empty,
            $"namespace {_projectNamespace}MasterDataLoaders.Installers",
            string.Empty,
            new OneLineComment(" ReSharper disable once UnusedType.Global"),
            new CodeBlock("public sealed class RepositoriesInstaller : IInstaller",
                "public int InstallPriority => 30",
                "public int ServiceUsePriority => 30",
                string.Empty,
                new CodeBlock("public void InstallServices(WebApplicationBuilder builder, string[] args)",
                    "Console.WriteLine(\"RepositoriesInstaller.InstallServices Started\")",
                    string.Empty,
                    $"builder.Services.AddScoped<IMasterDataRepository, {_projectShortName.Capitalize()}MasterDataRepository>()",
                    $"builder.Services.AddSingleton<{_projectShortName.Capitalize()}MasterDataRepoManager>()",
                    new OneLineComment(
                        "builder.Services.AddSingleton<IGmDbRepositoryCreatorFabric, GmDbRepositoryCreatorFabric>()"),
                    string.Empty,
                    "Console.WriteLine(\"RepositoriesInstaller.InstallServices Finished\")"),
                string.Empty,
                new CodeBlock("public void UseServices(WebApplication app)")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}