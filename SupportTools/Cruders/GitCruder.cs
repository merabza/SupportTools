using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibGitData.Models;
using LibGitWork;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SystemToolsShared;

namespace SupportTools.Cruders;

public sealed class GitCruder : ParCruder
{
    private readonly ILogger _logger;

    public GitCruder(ILogger logger, IParametersManager parametersManager) : base(parametersManager, "Git", "Gits")
    {
        _logger = logger;
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectAddress)));
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectFolderName)));
        FieldEditors.Add(new GitIgnorePathNameFieldEditor(logger, nameof(GitDataModel.GitIgnorePathName),
            ParametersManager, true));
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.Gits.ToDictionary(p => p.Key, p => (ItemData)p.Value);
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var gits = parameters.Gits;
        return gits.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not GitDataModel newGit)
            throw new Exception("newGit is null in GitCruder.UpdateRecordWithKey");

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Gits[recordKey] = newGit;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not GitDataModel newGit)
            throw new Exception("newGit is null in GitCruder.AddRecordWithKey");

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Gits.Add(recordKey, newGit);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var gits = parameters.Gits;
        gits.Remove(recordKey);
    }

    public override bool CheckValidation(ItemData item)
    {
        GitApi gitApi = new(true, _logger);
        try
        {
            if (item is not GitDataModel gitDataModel)
            {
                StShared.WriteErrorLine("item is not GitDataModel in GitCruder.CheckValidation", true, _logger);
                return false;
            }

            if (gitDataModel.GitProjectAddress is not null)
                return gitApi.IsGitRemoteAddressValid(gitDataModel.GitProjectAddress);

            StShared.WriteErrorLine("gitDataModel.GitProjectAddress is null in GitCruder.CheckValidation", true,
                _logger);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
            return false;
        }
    }

    public override string GetStatusFor(string name)
    {
        var git = (GitDataModel?)GetItemByName(name);
        if (git is null)
            return "ERROR: Git address Not found";
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projects = parameters.Projects;

        var usageCount = projects.Values.Count(project => project.GitProjectNames.Contains(name));

        return $"{git.GitProjectAddress} Usage count is: {usageCount}";
    }

    protected override ItemData CreateNewItem(ItemData? defaultItemData)
    {
        return new GitDataModel();
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        UpdateGitProjectCliMenuCommand updateGitProjectCommand = new(_logger, recordKey, ParametersManager);
        itemSubMenuSet.AddMenuItem(updateGitProjectCommand);

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projects = parameters.Projects;

        foreach (var itemSubMenuCommand in projects
                     .Where(projectKvp => projectKvp.Value.GitProjectNames.Contains(recordKey))
                     .Select(projectKvp => new InfoCliMenuCommand(projectKvp.Key, "/Main Menu/")))
            itemSubMenuSet.AddMenuItem(itemSubMenuCommand);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var updateGitProjectsCommand = new UpdateGitProjectsCliMenuCommand(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(updateGitProjectsCommand);
    }
}