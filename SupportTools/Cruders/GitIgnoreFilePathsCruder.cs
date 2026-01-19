using System;
using System.Collections.Generic;
using CliMenu;
using CliParameters.Cruders;
using LibGitData;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.CliMenuCommands.GitIgnoreFileTypes;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class GitIgnoreFilePathsCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly Dictionary<string, string> _currentValuesDict;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesWithDescriptionsFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public GitIgnoreFilePathsCruder(ILogger logger, IParametersManager parametersManager,
        Dictionary<string, string> currentValuesDict) : base("GitIgnore File Path", "GitIgnore File Paths", "Path")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _currentValuesDict = currentValuesDict;
    }

    public static GitIgnoreFilePathsCruder Create(ILogger logger, IParametersManager parametersManager)
    {
        return new GitIgnoreFilePathsCruder(logger, parametersManager,
            ((SupportToolsParameters)parametersManager.Parameters).GitIgnoreModelFilePaths);
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return _currentValuesDict;
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
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }

    public override string GetStatusFor(string name)
    {
        //var git = (GitDataModel?)GetItemByName(name);
        //if (git is null)
        //    return "ERROR: Git address Not found";
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
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

        var getDbServerFoldersCliMenuCommand =
            new ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand(_logger, recordKey,
                _parametersManager);
        itemSubMenuSet.AddMenuItem(getDbServerFoldersCliMenuCommand);
    }
}