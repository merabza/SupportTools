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
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace LibSupportToolsServerWork;

public sealed class GitFromServerCruder : Cruder
{
    private const string GitsList = nameof(GitsList);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IParametersManager _parametersManager;

    private GitFromServerCruder(ILogger logger, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache,
        IParametersManager parametersManager) : base("GitFromServer", "GitsFromServer")
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectAddress)));
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectFolderName)));
        //todo ეს აღსაღდგენი იქნება
        //FieldEditors.Add(new GitIgnorePathNameFieldEditor(logger, nameof(GitDataModel.GitIgnorePathName),
        //    ParametersManager, true));
    }

    public static GitFromServerCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, IParametersManager parametersManager)
    {
        return new GitFromServerCruder(logger, httpClientFactory, memoryCache, parametersManager);
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

    private List<GitDataDomain> GetGitReposFromServer()
    {
        return _memoryCache.GetOrCreate<List<GitDataDomain>>(GitsList, _ =>
        {
            try
            {
                var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

                var supportToolsServerApiClient =
                    supportToolsParameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);

                if (supportToolsServerApiClient is null)
                    return [];

                var remoteGitReposResult = supportToolsServerApiClient.GetGitRepos().Result;
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

        //try
        //{
        //    var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        //    var supportToolsServerApiClient =
        //        supportToolsParameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);

        //    if (supportToolsServerApiClient is null)
        //        return [];

        //    var remoteGitReposResult = supportToolsServerApiClient.GetGitRepos().Result;
        //    if (remoteGitReposResult.IsT0)
        //        return remoteGitReposResult.AsT0;

        //    StShared.WriteErrorLine("could not received remoteGits", true, _logger);
        //    Err.PrintErrorsOnConsole(remoteGitReposResult.AsT1);
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e);
        //    //throw;
        //}

        //return [];
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var gits = parameters.Gits;
        return gits.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not GitDataModel newGit)
            throw new Exception("newGit is null in GitCruder.UpdateRecordWithKey");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        parameters.Gits[recordKey] = newGit;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not GitDataModel newGit)
            throw new Exception("newGit is null in GitCruder.AddRecordWithKey");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        parameters.Gits.Add(recordKey, newGit);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
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