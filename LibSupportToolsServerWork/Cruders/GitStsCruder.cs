using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibGitData.Models;
using LibGitWork;
using LibParameters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OneOf;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace LibSupportToolsServerWork.Cruders;

public sealed class GitStsCruder : Cruder
{
    private const string GitsList = nameof(GitsList);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IParametersManager _parametersManager;

    private GitStsCruder(ILogger logger, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache,
        IParametersManager parametersManager) : base("GitFromServer", "GitsFromServer")
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectAddress)));
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectFolderName)));
        //FieldEditors.Add(new GitIgnorePathNameFieldEditor(logger, nameof(GitDataModel.GitIgnorePathName),
        //    ParametersManager, true));
    }

    public static GitStsCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, IParametersManager parametersManager)
    {
        return new GitStsCruder(logger, httpClientFactory, memoryCache, parametersManager);
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetGitReposFromServer().ToDictionary(k => k.GitProjectName,
            ItemData (v) => new GitDataModel
            {
                GitProjectAddress = v.GitProjectAddress,
                GitIgnorePathName = v.GitIgnorePathName,
                GitProjectFolderName = v.GitProjectFolderName
            });
    }

    private SupportToolsServerApiClient? GetSupportToolsServerApiClient()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        return supportToolsParameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
    }

    private List<GitDataDomain> GetGitReposFromServer()
    {
        return _memoryCache.GetOrCreate(GitsList, _ =>
        {
            var supportToolsServerApiClient = GetSupportToolsServerApiClient();

            if (supportToolsServerApiClient is null)
                return [];
            try
            {
                OneOf<List<GitDataDomain>, IEnumerable<Err>> remoteGitReposResult = supportToolsServerApiClient.GetGitRepos().Result;
                if (remoteGitReposResult.IsT0)
                    return remoteGitReposResult.AsT0;

                StShared.WriteErrorLine("could not received remoteGits", true, _logger);
                Err.PrintErrorsOnConsole(remoteGitReposResult.AsT1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }

            return [];
        }) ?? [];
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
            return false;

        try
        {
            var getGitRepoByKeyResult = supportToolsServerApiClient.GetGitRepoByKey(recordKey).Result;
            if (getGitRepoByKeyResult.IsT0) return true;

            if (getGitRepoByKeyResult.AsT1 is Err[] { Length: 1 } errors && errors[0].ErrorCode ==
                nameof(SupportToolsServerApiClientErrors.GitWithKeyNotFound))
                return false;

            Err.PrintErrorsOnConsole(getGitRepoByKeyResult.AsT1);

            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        AddOrUpdateRecordWithKey(recordKey, newRecord);
    }

    private void AddOrUpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        var supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true);
            return;
        }

        if (newRecord is not GitDataModel model)
        {
            StShared.WriteErrorLine("newRecord is not GitDataModel", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(model.GitIgnorePathName))
        {
            StShared.WriteErrorLine("GitIgnorePathName is not entered", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(model.GitProjectAddress))
        {
            StShared.WriteErrorLine("GitProjectAddress is not entered", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(model.GitProjectFolderName))
        {
            StShared.WriteErrorLine("GitProjectFolderName is not entered", true);
            return;
        }

        var gitDataDomain = new GitDataDomain
        {
            GitIgnorePathName = model.GitIgnorePathName,
            GitProjectAddress = model.GitProjectAddress,
            GitProjectFolderName = model.GitProjectFolderName,
            GitProjectName = recordKey
        };

        try
        {
            var updateGitRepoByKeyResult = supportToolsServerApiClient
                .UpdateGitRepoByKey(recordKey, gitDataDomain).Result;
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
        AddOrUpdateRecordWithKey(recordKey, newRecord);
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
            var updateGitRepoByKeyResult = supportToolsServerApiClient.RemoveGitRepoByKey(recordKey).Result;
            if (updateGitRepoByKeyResult.IsSome)
                Err.PrintErrorsOnConsole((Err[])updateGitRepoByKeyResult);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
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
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var projects = parameters.Projects;

        var usageCount = projects.Values.Count(project => project.GitProjectNames.Contains(name)) +
                         projects.Values.Count(project => project.ScaffoldSeederGitProjectNames.Contains(name));

        return $"{git.GitProjectAddress} Usage count is: {usageCount}";
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new GitDataModel();
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        //base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        //UpdateGitProjectCliMenuCommand updateGitProjectCommand = new(_logger, recordKey, _parametersManager);
        //itemSubMenuSet.AddMenuItem(updateGitProjectCommand);

        //var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        //var projects = parameters.Projects;

        ////Main Menu/
        //foreach (var itemSubMenuCommand in projects
        //             .Where(projectKvp => projectKvp.Value.GitProjectNames.Contains(recordKey)).Select(projectKvp =>
        //                 new InfoCliMenuCommand(projectKvp.Key,
        //                     $"{projectKvp.Value.ProjectGroupName}/{projectKvp.Key}")).OrderBy(x => x.Name))
        //    itemSubMenuSet.AddMenuItem(itemSubMenuCommand);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        //var updateGitProjectsCommand = new UpdateGitProjectsCliMenuCommand(_logger, _parametersManager);
        //cruderSubMenuSet.AddMenuItem(updateGitProjectsCommand);

        //var uploadGitProjectsToSupportToolsServerCliMenuCommand =
        //    new UploadGitProjectsToSupportToolsServerCliMenuCommand(_logger, _httpClientFactory, _parametersManager);
        //cruderSubMenuSet.AddMenuItem(uploadGitProjectsToSupportToolsServerCliMenuCommand);
    }
}