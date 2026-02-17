using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.Mappers;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using ToolsManagement.LibToolActions;

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

        var gitIgnoreFiles = new List<StsGitIgnoreFileTypeDataModel>();
        foreach ((string key, string fileName) in supportToolsParameters.GitIgnoreModelFilePaths)
        {
            string content = File.Exists(fileName)
                ? await File.ReadAllTextAsync(fileName, cancellationToken)
                : string.Empty;

            gitIgnoreFiles.Add(new StsGitIgnoreFileTypeDataModel { Name = key, Content = content });
        }

        var gitRepos = GitRepos.Create(Logger, supportToolsParameters.Gits, null, UseConsole, true);

        await supportToolsServerApiClient.UploadGitRepos(
            new SyncGitRequest
            {
                GitIgnoreFiles = gitIgnoreFiles,
                Gits = gitRepos.Gits.Values.Select(g => g.ToContractModel()).ToList()
            }, cancellationToken);

        ////თითოეული გიტის პროექტი აიტვირთოს სერვერზე

        //foreach (var gitRepo in gitRepos.Gits)
        //{
        //    supportToolsServerWebApiClient
        //}

        return true;
    }
}
