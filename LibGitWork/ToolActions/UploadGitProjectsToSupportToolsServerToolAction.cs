using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

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
        var supportToolsServerApiClient =
            supportToolsParameters.GetSupportToolsServerApiClient(Logger, _httpClientFactory);

        if (supportToolsServerApiClient == null)
            return false;

        var gitIgnoreFiles = new List<GitIgnoreFile>();
        foreach (var (key, fileName) in supportToolsParameters.GitIgnoreModelFilePaths)
        {
            string content;
            if (File.Exists(fileName))
                content = await File.ReadAllTextAsync(fileName, cancellationToken);
            else
                content = string.Empty;
            gitIgnoreFiles.Add(new GitIgnoreFile { Name = key, Content = content });
        }

        var gitRepos = GitRepos.Create(Logger, supportToolsParameters.Gits, null, UseConsole, true);

        await supportToolsServerApiClient.UploadGitRepos(
            new SyncGitRequest { GitIgnoreFiles = gitIgnoreFiles, Gits = gitRepos.Gits }, cancellationToken);

        ////თითოეული გიტის პროექტი აიტვირთოს სერვერზე

        //foreach (var gitRepo in gitRepos.Gits)
        //{
        //    supportToolsServerWebApiClient
        //}

        return true;
    }
}