using System;
using System.Collections.Generic;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Cruders;
using LibGitData;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.CliMenuCommands.GitIgnoreFileTypes;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class GitIgnoreModelsCruder : SimpleNamesListCruder
{
    private readonly List<string> _currentValuesList;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesListFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public GitIgnoreModelsCruder(ILogger logger, IParametersManager parametersManager,
        List<string> currentValuesList) : base("GitIgnore Model", "GitIgnore Models")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _currentValuesList = currentValuesList;
    }

    public static GitIgnoreModelsCruder Create(ILogger logger, IParametersManager parametersManager)
    {
        return new GitIgnoreModelsCruder(logger, parametersManager,
            ((SupportToolsParameters)parametersManager.Parameters).GitIgnoreModels);
    }

    protected override List<string> GetList()
    {
        return _currentValuesList;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var checkGitIgnoreFilesCliMenuCommand = new CheckGitIgnoreFilesCliMenuCommand(_logger, _parametersManager);
        cruderSubMenuSet.AddMenuItem(checkGitIgnoreFilesCliMenuCommand);

        var updateGitIgnoreFilesCliMenuCommand = new UpdateGitIgnoreFilesCliMenuCommand(_logger, _parametersManager);
        cruderSubMenuSet.AddMenuItem(updateGitIgnoreFilesCliMenuCommand);

        var generateCommand = new GenerateStandardGitignoreFilesCliMenuCommand(_logger, _parametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);

        var syncUpCommand = new SyncUpGitignoreFilesCliMenuCommand(_logger, _parametersManager);
        cruderSubMenuSet.AddMenuItem(syncUpCommand);
    }

    public override string GetStatusFor(string name)
    {
        //var git = (GitDataModel?)GetItemByName(name);
        //if (git is null)
        //    return "ERROR: Git address Not found";
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        Dictionary<string, ProjectModel> projects = supportToolsParameters.Projects;

        int usageCount = 0;

        foreach ((string _, ProjectModel project) in projects)
        {
            foreach (EGitCol gitCol in Enum.GetValues<EGitCol>())
            {
                Dictionary<string, GitDataModel> gits = supportToolsParameters.Gits;
                List<string> gitProjectNames = gitCol switch
                {
                    EGitCol.Main => project.GitProjectNames,
                    EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
                    _ => null
                } ?? [];

                foreach (string gitProjectName in gitProjectNames)
                {
                    if (!gits.TryGetValue(gitProjectName, out GitDataModel? git))
                    {
                        continue;
                    }

                    if (git.GitIgnorePathName == name)
                    {
                        usageCount++;
                    }
                }
            }
        }

        return $"Usage count is: {usageCount}";
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        var getDbServerFoldersCliMenuCommand =
            new ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand(_logger, itemName,
                _parametersManager);
        itemSubMenuSet.AddMenuItem(getDbServerFoldersCliMenuCommand);
    }
}
