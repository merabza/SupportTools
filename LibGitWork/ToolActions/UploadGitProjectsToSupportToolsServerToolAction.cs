using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using SystemTools.BackgroundTasks;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibGitWork.ToolActions;

public sealed class UploadGitProjectsToSupportToolsServerToolAction : ToolAction
{
    public const string ActionName = "Upload Git Projects To SupportToolsServer";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadGitProjectsToSupportToolsServerToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, bool useConsole) : base(logger, ActionName, null, null, useConsole)
    {
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        SupportToolsServerApiClient? supportToolsServerApiClient =
            supportToolsParameters.GetSupportToolsServerApiClient(Logger, _httpClientFactory);

        if (supportToolsServerApiClient == null)
        {
            return false;
        }

        string? folderForGitignoreFiles = supportToolsParameters.FolderForGitignoreFiles;
        if (string.IsNullOrWhiteSpace(folderForGitignoreFiles))
        {
            StShared.WriteErrorLine("supportToolsParameters.FolderForGitignoreFiles is empty", true, Logger);
            return false;
        }

        var gitIgnoreFiles = new List<StsGitIgnoreFileTypeDataModel>();
        foreach (string key in supportToolsParameters.GitIgnorePatterns)
        {
            string fileName = SupportToolsParameters.GetGitIgnoreModelFilePath(folderForGitignoreFiles, key);
            string content = File.Exists(fileName)
                ? await File.ReadAllTextAsync(fileName, cancellationToken)
                : string.Empty;

            gitIgnoreFiles.Add(new StsGitIgnoreFileTypeDataModel { Name = key, Content = content });
        }

        //GitRepos.Create აქ არ გამოდგება: ის სახელს საქაღალდის სახელით ცვლის, შაბლონურ საქაღალდეს კი
        //ჩანაწერის სახელით. სერვერზე ორივე ისე უნდა შეინახოს, როგორც პარამეტრებშია
        var gits = new List<StsGitDataModel>();
        foreach ((string gitProjectName, GitDataModel gitData) in supportToolsParameters.Gits)
        {
            if (string.IsNullOrWhiteSpace(gitData.GitProjectAddress) ||
                string.IsNullOrWhiteSpace(gitData.GitProjectFolderName) ||
                string.IsNullOrWhiteSpace(gitData.GitIgnorePatternName))
            {
                StShared.WriteErrorLine($"Git Repo with key {gitProjectName} is not fully filled and is not uploaded",
                    UseConsole, Logger);
                continue;
            }

            gits.Add(new StsGitDataModel
            {
                GitProjectName = gitProjectName,
                GitProjectAddress = gitData.GitProjectAddress,
                GitProjectFolderName = gitData.GitProjectFolderName,
                GitIgnorePatternName = gitData.GitIgnorePatternName
            });
        }

        Result result = await supportToolsServerApiClient.UploadGitRepos(
            new SyncGitRequest { GitIgnoreFiles = gitIgnoreFiles, Gits = gits }, cancellationToken);
        if (result.IsFailure)
        {
            result.Error.PrintErrorsOnConsole();
            return false;
        }

        ////თითოეული გიტის პროექტი აიტვირთოს სერვერზე

        //foreach (var gitRepo in gitRepos.Gits)
        //{
        //    supportToolsServerWebApiClient
        //}

        return true;
    }
}
