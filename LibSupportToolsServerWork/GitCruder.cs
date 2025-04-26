﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using CliMenu;
//using CliParameters;
//using CliParameters.FieldEditors;
//using CliTools.CliMenuCommands;
//using LibGitData.Models;
//using LibParameters;
//using Microsoft.Extensions.Logging;
//using SupportToolsData.Models;
//using SupportToolsServerApiContracts.Models;
//using SystemToolsShared;
//using SystemToolsShared.Errors;

//namespace LibSupportToolsServerWork;

//public sealed class GitFromServerCruder : Cruder
//{
//    private readonly IHttpClientFactory _httpClientFactory;
//    private readonly ILogger _logger;
//    private readonly List<GitDataDomain> _remoteGitRepos;

//    private GitFromServerCruder(ILogger logger, IHttpClientFactory httpClientFactory, IParametersManager parametersManager,
//        List<GitDataDomain> remoteGitRepos) : base(parametersManager, "Git", "Gits")
//    {
//        _logger = logger;
//        _httpClientFactory = httpClientFactory;
//        _remoteGitRepos = remoteGitRepos;
//        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectAddress)));
//        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectFolderName)));
//        //todo ეს აღსაღდგენი იქნება
//        //FieldEditors.Add(new GitIgnorePathNameFieldEditor(logger, nameof(GitDataModel.GitIgnorePathName),
//        //    ParametersManager, true));
//    }

//    public static GitFromServerCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
//        IParametersManager parametersManager)
//    {
//        List<GitDataDomain> remoteGitRepos = [];
//        try
//        {
//            var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

//            var supportToolsServerApiClient =
//                supportToolsParameters.GetSupportToolsServerApiClient(logger, httpClientFactory);

//            if (supportToolsServerApiClient is null)
//                return new GitFromServerCruder(logger, httpClientFactory, parametersManager, remoteGitRepos);
//            var remoteGitReposResult = supportToolsServerApiClient.GetGitRepos().Result;
//            if (remoteGitReposResult.IsT0)
//            {
//                remoteGitRepos = remoteGitReposResult.AsT0;
//            }
//            else
//            {
//                StShared.WriteErrorLine("could not received remoteGits", true, logger);
//                Err.PrintErrorsOnConsole(remoteGitReposResult.AsT1);
//            }
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine(e);
//            //throw;
//        }

//        return new GitFromServerCruder(logger, httpClientFactory, parametersManager, remoteGitRepos);
//    }

//    protected override Dictionary<string, ItemData> GetCrudersDictionary()
//    {
//        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
//        return parameters.Gits.ToDictionary(p => p.Key, ItemData (p) => p.Value);
//    }

//    public override bool ContainsRecordWithKey(string recordKey)
//    {
//        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
//        var gits = parameters.Gits;
//        return gits.ContainsKey(recordKey);
//    }

//    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
//    {
//        if (newRecord is not GitDataModel newGit)
//            throw new Exception("newGit is null in GitCruder.UpdateRecordWithKey");

//        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
//        parameters.Gits[recordKey] = newGit;
//    }

//    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
//    {
//        if (newRecord is not GitDataModel newGit)
//            throw new Exception("newGit is null in GitCruder.AddRecordWithKey");

//        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
//        parameters.Gits.Add(recordKey, newGit);
//    }

//    protected override void RemoveRecordWithKey(string recordKey)
//    {
//        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
//        var gits = parameters.Gits;
//        gits.Remove(recordKey);
//    }

//    public override bool CheckValidation(ItemData item)
//    {
//        GitApi gitApi = new(true, _logger);
//        try
//        {
//            if (item is not GitDataModel gitDataModel)
//            {
//                StShared.WriteErrorLine("item is not GitDataModel in GitCruder.CheckValidation", true, _logger);
//                return false;
//            }

//            if (gitDataModel.GitProjectAddress is not null)
//                return gitApi.IsGitRemoteAddressValid(gitDataModel.GitProjectAddress);

//            StShared.WriteErrorLine("gitDataModel.GitProjectAddress is null in GitCruder.CheckValidation", true,
//                _logger);
//            return false;
//        }
//        catch (Exception e)
//        {
//            _logger.LogError(e, null);
//            return false;
//        }
//    }

//    public override string GetStatusFor(string name)
//    {
//        var git = (GitDataModel?)GetItemByName(name);
//        if (git is null)
//            return "ERROR: Git address Not found";
//        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
//        var projects = parameters.Projects;

//        var usageCount = projects.Values.Count(project => project.GitProjectNames.Contains(name)) +
//                         projects.Values.Count(project => project.ScaffoldSeederGitProjectNames.Contains(name));

//        var remGitRepo = _remoteGitRepos.SingleOrDefault(x => x.GitProjectAddress == git.GitProjectAddress);

//        return
//            $"{git.GitProjectAddress} Usage count is: {usageCount}{(remGitRepo is null ? "" : $"- rem name is: {remGitRepo.GitProjectName}")}";
//    }

//    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
//    {
//        return new GitDataModel();
//    }

//    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
//    {
//        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

//        UpdateGitProjectCliMenuCommand updateGitProjectCommand = new(_logger, recordKey, ParametersManager);
//        itemSubMenuSet.AddMenuItem(updateGitProjectCommand);

//        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
//        var projects = parameters.Projects;

//        //Main Menu/
//        foreach (var itemSubMenuCommand in projects
//                     .Where(projectKvp => projectKvp.Value.GitProjectNames.Contains(recordKey)).Select(projectKvp =>
//                         new InfoCliMenuCommand(projectKvp.Key,
//                             $"{projectKvp.Value.ProjectGroupName}/{projectKvp.Key}")).OrderBy(x => x.Name))
//            itemSubMenuSet.AddMenuItem(itemSubMenuCommand);
//    }

//    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
//    {
//        var updateGitProjectsCommand = new UpdateGitProjectsCliMenuCommand(_logger, ParametersManager);
//        cruderSubMenuSet.AddMenuItem(updateGitProjectsCommand);

//        var uploadGitProjectsToSupportToolsServerCliMenuCommand =
//            new UploadGitProjectsToSupportToolsServerCliMenuCommand(_logger, _httpClientFactory, ParametersManager);
//        cruderSubMenuSet.AddMenuItem(uploadGitProjectsToSupportToolsServerCliMenuCommand);
//    }
//}