using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibAppProjectCreator.Git;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.Cruders;

public sealed class GitCruder : ParCruder
{
    private readonly ILogger _logger;

    public GitCruder(ILogger logger, ParametersManager parametersManager) : base(parametersManager, "Git", "Gits")
    {
        _logger = logger;
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectAddress)));
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectFolderName)));
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

    public override void UpdateRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not GitDataModel newGit)
            throw new Exception("newGit is null in GitCruder.UpdateRecordWithKey");

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Gits[recordName] = newGit;
    }

    protected override void AddRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not GitDataModel newGit)
            throw new Exception("newGit is null in GitCruder.AddRecordWithKey");

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Gits.Add(recordName, newGit);
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

    public override string? GetStatusFor(string name)
    {
        var git = (GitDataModel?)GetItemByName(name);
        return git?.GitProjectAddress;
    }

    protected override ItemData CreateNewItem(string recordName, ItemData? defaultItemData)
    {
        return new GitDataModel();
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordName);

        UpdateGitProjectCliMenuCommand updateGitProjectCommand = new(_logger, recordName, ParametersManager);
        itemSubMenuSet.AddMenuItem(updateGitProjectCommand);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var updateGitProjectsCommand =
            UpdateGitProjectsCliMenuCommand.Create(_logger, ParametersManager);
        if (updateGitProjectsCommand is null)
            return;
        cruderSubMenuSet.AddMenuItem(updateGitProjectsCommand);
    }
}