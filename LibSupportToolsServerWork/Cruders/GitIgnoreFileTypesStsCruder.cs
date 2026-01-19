using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using CliParameters;
using CliParameters.Cruders;
using CliParameters.FieldEditors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace LibSupportToolsServerWork.Cruders;

public sealed class GitIgnoreFileTypesStsCruder : Cruder
{
    private const string GitIgnoreFileTypesList = nameof(GitIgnoreFileTypesList);
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IParametersManager _parametersManager;

    public GitIgnoreFileTypesStsCruder(ILogger logger, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache,
        IParametersManager parametersManager) : base("GitIgnore File Type", "GitIgnore File Types")
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
        FieldEditors.Add(new TextFieldEditor(nameof(TextItemData.Text), null, true));
    }

    public static GitIgnoreFileTypesStsCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, IParametersManager parametersManager)
    {
        return new GitIgnoreFileTypesStsCruder(logger, httpClientFactory, memoryCache, parametersManager);
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetGitIgnoreFileTypesListFromServer().ToDictionary(k => k.Name,
            ItemData (v) => new TextItemData { Text = v.Name });
    }

    private SupportToolsServerApiClient? GetSupportToolsServerApiClient()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        return supportToolsParameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
    }

    private List<StsGitIgnoreFileTypeDataModel> GetGitIgnoreFileTypesListFromServer()
    {
        return _memoryCache.GetOrCreate(GitIgnoreFileTypesList, _ =>
        {
            var supportToolsServerApiClient = GetSupportToolsServerApiClient();

            if (supportToolsServerApiClient is null)
                return [];
            try
            {
                var remoteGitReposResult = supportToolsServerApiClient.GetGitIgnoreFileTypesList().Result;
                if (remoteGitReposResult.IsT0)
                    return remoteGitReposResult.AsT0;

                StShared.WriteErrorLine("could not received GitIgnore File Types List", true, _logger);
                Err.PrintErrorsOnConsole(remoteGitReposResult.AsT1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return [];
        }) ?? [];
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var gitIgnoreModelFilePaths = GetGitIgnoreFileTypesListFromServer();
        return gitIgnoreModelFilePaths.Any(x => x.Name == recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        AddOrUpdateRecordWithKey(recordKey);
    }

    private void AddOrUpdateRecordWithKey(string recordKey)
    {
        var supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true);
            return;
        }

        try
        {
            var updateGitRepoByKeyResult = supportToolsServerApiClient
                .UpdateGitIgnoreFileType(recordKey, CancellationToken.None).Result;
            if (updateGitRepoByKeyResult.IsSome)
                Err.PrintErrorsOnConsole((Err[])updateGitRepoByKeyResult);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        AddOrUpdateRecordWithKey(recordKey);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true);
            return;
        }

        try
        {
            var updateGitRepoByKeyResult = supportToolsServerApiClient.RemoveGitIgnoreFileTypeName(recordKey).Result;
            if (updateGitRepoByKeyResult.IsSome)
                Err.PrintErrorsOnConsole((Err[])updateGitRepoByKeyResult);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        //var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        //var gits = parameters.GitIgnoreModelFilePaths;
        //gits.Remove(recordKey);
    }

    //private Dictionary<string, string> GetGitIgnoreFilePaths()
    //{
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    return parameters.GitIgnoreModelFilePaths;
    //}

    //protected override Dictionary<string, ItemData> GetCrudersDictionary()
    //{
    //    var gitIgnoreModelFilePaths = GetGitIgnoreFilePaths();
    //    return gitIgnoreModelFilePaths.ToDictionary(k => k.Key, ItemData (v) => new TextItemData { Text = v.Value });
    //}

    //public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    if (newRecord is not TextItemData newGitIgnoreModelFilePath)
    //        throw new Exception("newRecord is null in GitIgnoreFilePathsCruder.UpdateRecordWithKey");
    //    if (string.IsNullOrWhiteSpace(newGitIgnoreModelFilePath.Text))
    //        throw new Exception("newRecord.Text is empty in GitIgnoreFilePathsCruder.UpdateRecordWithKey");
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    parameters.GitIgnoreModelFilePaths[recordKey] = newGitIgnoreModelFilePath.Text;
    //}

    //protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    if (newRecord is not TextItemData newGitIgnoreModelFilePath)
    //        throw new Exception("newGitIgnoreModelFilePath is null in GitIgnoreFilePathsCruder.AddRecordWithKey");
    //    if (string.IsNullOrWhiteSpace(newGitIgnoreModelFilePath.Text))
    //        throw new Exception("newGitIgnoreModelFilePath.Text is empty in GitIgnoreFilePathsCruder.AddRecordWithKey");
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    parameters.GitIgnoreModelFilePaths.Add(recordKey, newGitIgnoreModelFilePath.Text);
    //}

    //protected override void RemoveRecordWithKey(string recordKey)
    //{
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    var gitIgnoreModelFilePaths = parameters.GitIgnoreModelFilePaths;
    //    gitIgnoreModelFilePaths.Remove(recordKey);
    //}

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    //protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    //{
    //    var checkGitIgnoreFilesCliMenuCommand = new CheckGitIgnoreFilesCliMenuCommand(_logger, ParametersManager);
    //    cruderSubMenuSet.AddMenuItem(checkGitIgnoreFilesCliMenuCommand);

    //    var updateGitIgnoreFilesCliMenuCommand = new UpdateGitIgnoreFilesCliMenuCommand(_logger, ParametersManager);
    //    cruderSubMenuSet.AddMenuItem(updateGitIgnoreFilesCliMenuCommand);

    //    GenerateStandardGitignoreFilesCliMenuCommand generateCommand = new(_logger, ParametersManager);
    //    cruderSubMenuSet.AddMenuItem(generateCommand);
    //}

    //public override string GetStatusFor(string name)
    //{
    //    //var git = (GitDataModel?)GetItemByName(name);
    //    //if (git is null)
    //    //    return "ERROR: Git address Not found";
    //    var supportToolsParameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    var projects = supportToolsParameters.Projects;

    //    var usageCount = 0;

    //    foreach (var (_, project) in projects)
    //    foreach (var gitCol in Enum.GetValues<EGitCol>())
    //    {
    //        var gits = supportToolsParameters.Gits;
    //        var gitProjectNames = gitCol switch
    //        {
    //            EGitCol.Main => project.GitProjectNames,
    //            EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
    //            _ => null
    //        } ?? [];

    //        foreach (var gitProjectName in gitProjectNames)
    //        {
    //            if (!gits.TryGetValue(gitProjectName, out var git))
    //                continue;
    //            if (git.GitIgnorePathName == name)
    //                usageCount++;
    //        }
    //    }

    //    return $"Usage count is: {usageCount}";
    //}

    //public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    //{
    //    base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

    //    ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand getDbServerFoldersCliMenuCommand =
    //        new(_logger, recordKey, ParametersManager);
    //    itemSubMenuSet.AddMenuItem(getDbServerFoldersCliMenuCommand);
    //}
}