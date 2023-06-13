using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators.CarcassAndDatabase;

public sealed class ProjectMasterDataRepositoryClassCreator : CodeCreator
{
    private readonly string _projectName;
    private readonly string _projectShortName;

    public ProjectMasterDataRepositoryClassCreator(ILogger logger, string placePath, string projectName,
        string projectShortName, string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectName = projectName;
        _projectShortName = projectShortName;
    }

    public override void CreateFileStructure()
    {
        var projectMasterDataRepositoryClassName = $"{_projectShortName.Capitalize()}MasterDataRepository";
        var projectDbContextClassName = $"{_projectName}DbContext";
        var dbContextObjectName = $"_{_projectShortName.UnCapitalize()}Ctx";

        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CarcassIdentity",
            "using CarcassRepositories",
            "using CarcassRepositories.MasterDataLoaders",
            $"using {_projectName}Db",
            "using Microsoft.AspNetCore.Identity",
            "using Microsoft.Extensions.Logging",
            "",
            $"namespace {_projectName}MasterDataLoaders",
            "",
            new CodeBlock($"public sealed class {projectMasterDataRepositoryClassName} : MasterDataRepository",
                $"private readonly {projectDbContextClassName} {dbContextObjectName}",
                $"private readonly {_projectShortName.Capitalize()}MasterDataRepoManager _{_projectShortName.UnCapitalize()}MasterDataRepoManager",
                "",
                new CodeBlock(
                    $"public {projectMasterDataRepositoryClassName}({projectDbContextClassName} ctx, RoleManager<AppRole> roleManager, UserManager<AppUser> userManager, ILogger<MasterDataRepository> logger, {_projectShortName.Capitalize()}MasterDataRepoManager {_projectShortName.UnCapitalize()}MasterDataRepoManager) : base(ctx, roleManager, userManager, logger)",
                    $"{dbContextObjectName} = ctx",
                    $"_{_projectShortName.UnCapitalize()}MasterDataRepoManager = {_projectShortName.UnCapitalize()}MasterDataRepoManager"),
                "",
                new CodeBlock("protected override ICustomMdLoader? GetSpecificLoader(string tableName)",
                    $"I{_projectShortName.Capitalize()}MdLoaderCreator? creator = _{_projectShortName.UnCapitalize()}MasterDataRepoManager.GetLoaderCreator(tableName)",
                    $"return creator?.Create({dbContextObjectName}) ?? base.GetSpecificLoader(tableName)")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}