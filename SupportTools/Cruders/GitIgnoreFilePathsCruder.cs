using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibGitData;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SupportTools.Cruders;

public sealed class GitIgnoreFilePathsCruder : ParCruder
{
    private readonly ILogger _logger;

    public GitIgnoreFilePathsCruder(ILogger logger, IParametersManager parametersManager) : base(parametersManager,
        "GitIgnore File Path", "GitIgnore File Paths")
    {
        _logger = logger;
        FieldEditors.Add(new FilePathFieldEditor(nameof(TextItemData.Text), null, true));
    }

    private Dictionary<string, string> GetGitIgnoreFilePaths()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.GitIgnoreModelFilePaths;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var gitIgnoreModelFilePaths = GetGitIgnoreFilePaths();
        return gitIgnoreModelFilePaths.ToDictionary(k => k.Key, v => (ItemData)new TextItemData { Text = v.Value });
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var gitIgnoreModelFilePaths = GetGitIgnoreFilePaths();
        return gitIgnoreModelFilePaths.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newGitIgnoreModelFilePath)
            throw new Exception("newRecord is null in GitIgnoreFilePathsCruder.UpdateRecordWithKey");
        if (string.IsNullOrWhiteSpace(newGitIgnoreModelFilePath.Text))
            throw new Exception("newRecord.Text is empty in GitIgnoreFilePathsCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.GitIgnoreModelFilePaths[recordKey] = newGitIgnoreModelFilePath.Text;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newGitIgnoreModelFilePath)
            throw new Exception("newGitIgnoreModelFilePath is null in GitIgnoreFilePathsCruder.AddRecordWithKey");
        if (string.IsNullOrWhiteSpace(newGitIgnoreModelFilePath.Text))
            throw new Exception("newGitIgnoreModelFilePath.Text is empty in GitIgnoreFilePathsCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.GitIgnoreModelFilePaths.Add(recordKey, newGitIgnoreModelFilePath.Text);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var gitIgnoreModelFilePaths = parameters.GitIgnoreModelFilePaths;
        gitIgnoreModelFilePaths.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var checkGitIgnoreFilesCliMenuCommand = new CheckGitIgnoreFilesCliMenuCommand(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(checkGitIgnoreFilesCliMenuCommand);

        var updateGitIgnoreFilesCliMenuCommand = new UpdateGitIgnoreFilesCliMenuCommand(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(updateGitIgnoreFilesCliMenuCommand);

        GenerateStandardGitignoreFilesCliMenuCommand generateCommand = new(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand, "Generate standard .gitignore files...");
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
        {
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
        }

        return $"Usage count is: {usageCount}";
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand getDbServerFoldersCliMenuCommand = new(_logger, recordKey, ParametersManager);
        itemSubMenuSet.AddMenuItem(getDbServerFoldersCliMenuCommand,
            "Apply this file type to all projects that do not have a type specified");
    }
}