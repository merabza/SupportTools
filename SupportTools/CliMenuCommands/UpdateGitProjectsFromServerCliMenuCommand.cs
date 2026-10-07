using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork;
using LibSupportToolsServerWork.GitProjects;
using LibSupportToolsServerWork.Registry.Paths;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

//„Update Git Projects“-ის ალტერნატივა, რომელიც git-ებს არ კლონავს: GitProjects SupportToolsServer-იდან ჩამოდის, რომელიც
//მათ თავისი კლონებიდან ითვლის (SupportToolsServer-ის B9). შედეგი ლოკალური სკანირების შედეგის იდენტურია
//(ServerGitProjectsMapper) და GitProjects-ს მთლიანად ანაცვლებს. ცარიელი სია (სერვერს რეპოზიტორიები ჯერ არ
//დაუსკანერებია) ლოკალურ GitProjects-ს არ ცვლის
public sealed class UpdateGitProjectsFromServerCliMenuCommand : CliMenuCommand
{
    public const string MenuName = "Update Git Projects From SupportToolsServer";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitProjectsFromServerCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base(MenuName, EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        SupportToolsServerApiClient? supportToolsServerApiClient =
            parameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true, _logger);
            return false;
        }

        Result<List<StsGitProjectDataModel>> serverGitProjectsResult =
            await supportToolsServerApiClient.GetGitProjects(cancellationToken);
        if (serverGitProjectsResult.IsFailure)
        {
            serverGitProjectsResult.Error.PrintErrorsOnConsole();
            return false;
        }

        if (serverGitProjectsResult.Value.Count == 0)
        {
            StShared.WriteErrorLine("SupportToolsServer has no git projects yet, local GitProjects are not changed",
                true, _logger);
            return false;
        }

        //ლოკალური სკანირების git-ები: GitRepos.Create არასრულ ჩანაწერებს იმავე წესით ტოვებს
        var gitRepos = GitRepos.Create(_logger, parameters.Gits, null, true, true);
        ServerGitProjects serverGitProjects = ServerGitProjectsMapper.ToLocal(serverGitProjectsResult.Value,
            gitRepos.Gits.Keys, new PathMapper(parameters.PathMappings));

        foreach (string gitName in serverGitProjects.UnknownGitNames)
        {
            StShared.WriteWarningLine($"Git {gitName} is not in the local Gits, its projects are skipped", true,
                _logger);
        }

        foreach (GitProjectDuplicate duplicate in serverGitProjects.Duplicates)
        {
            StShared.WriteWarningLine(
                $"Git project {duplicate.ProjectName} is in {duplicate.ReplacedGitName} and in {duplicate.GitName}, the one of {duplicate.GitName} is used",
                true, _logger);
        }

        (int added, int changed, int removed) =
            ServerGitProjectsMapper.CountChanges(parameters.GitProjects, serverGitProjects.GitProjects);

        //GitProjects init-only-ა, ამიტომ ეგზემპლარი იგივე რჩება და შიგთავსი იცვლება
        parameters.GitProjects.Clear();
        foreach ((string projectName, var gitProject) in serverGitProjects.GitProjects)
        {
            parameters.GitProjects.Add(projectName, gitProject);
        }

        await _parametersManager.Save(parameters, "Git Projects Updated From SupportToolsServer", null,
            cancellationToken);

        Console.WriteLine(
            $"{parameters.GitProjects.Count} git projects received from SupportToolsServer: {added} added, {changed} changed, {removed} removed");
        return true;
    }
}
