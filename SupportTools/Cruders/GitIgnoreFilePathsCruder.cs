using System;
using System.Collections.Generic;
using CliMenu;
using LibGitData;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class GitIgnoreFilePathsCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnoreFilePathsCruder(ILogger logger, IParametersManager parametersManager) : base(parametersManager,
        "GitIgnore File Path", "GitIgnore File Paths", "Path")
    {
        _logger = logger;
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)ParametersManager.Parameters).GitIgnoreModelFilePaths;
    }


    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var checkGitIgnoreFilesCliMenuCommand = new CheckGitIgnoreFilesCliMenuCommand(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(checkGitIgnoreFilesCliMenuCommand);

        var updateGitIgnoreFilesCliMenuCommand = new UpdateGitIgnoreFilesCliMenuCommand(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(updateGitIgnoreFilesCliMenuCommand);

        GenerateStandardGitignoreFilesCliMenuCommand generateCommand = new(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }

    public override string GetStatusFor(string name)
    {
        //var git = (GitDataModel?)GetItemByName(name);
        //if (git is null)
        //    return "ERROR: Git address Not found";
        var supportToolsParameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projects = supportToolsParameters.Projects;

        var usageCount = 0;

        foreach (var (_, project) in projects)
        foreach (var gitCol in Enum.GetValues<EGitCol>())
        {
            var gits = supportToolsParameters.Gits;
            var gitProjectNames = gitCol switch
            {
                EGitCol.Main => project.GitProjectNames,
                EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
                _ => null
            } ?? [];

            foreach (var gitProjectName in gitProjectNames)
            {
                if (!gits.TryGetValue(gitProjectName, out var git))
                    continue;
                if (git.GitIgnorePathName == name)
                    usageCount++;
            }
        }

        return $"Usage count is: {usageCount}";
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand getDbServerFoldersCliMenuCommand =
            new(_logger, recordKey, ParametersManager);
        itemSubMenuSet.AddMenuItem(getDbServerFoldersCliMenuCommand);
    }
}