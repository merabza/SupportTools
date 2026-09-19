using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

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
            SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();

            if (supportToolsServerApiClient is null)
            {
                return [];
            }

            try
            {
                Result<List<StsGitIgnoreFileTypeDataModel>> remoteGitReposResult =
                    supportToolsServerApiClient.GetGitIgnoreFileTypesList().Result;
                if (remoteGitReposResult.IsSuccess)
                {
                    return remoteGitReposResult.Value;
                }

                StShared.WriteErrorLine("could not received GitIgnore File Types List", true, _logger);
                remoteGitReposResult.Error.PrintErrorsOnConsole();
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
        List<StsGitIgnoreFileTypeDataModel> gitIgnoreModelFilePaths = GetGitIgnoreFileTypesListFromServer();
        return gitIgnoreModelFilePaths.Any(x => x.Name == recordKey);
    }

    public override ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        AddOrUpdateRecordWithKey(recordKey);
        return ValueTask.CompletedTask;
    }

    private void AddOrUpdateRecordWithKey(string recordKey)
    {
        SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true);
            return;
        }

        try
        {
            Result updateGitRepoByKeyResult = supportToolsServerApiClient
                .UpdateGitIgnoreFileType(recordKey, CancellationToken.None).Result;
            if (updateGitRepoByKeyResult.IsFailure)
            {
                updateGitRepoByKeyResult.Error.PrintErrorsOnConsole();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        return UpdateRecordWithKey(recordKey, newRecord, cancellationToken);
    }

    protected override async ValueTask RemoveRecordWithKey(string recordKey,
        CancellationToken cancellationToken = default)
    {
        SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true);
            return;
        }

        try
        {
            Result updateGitRepoByKeyResult =
                await supportToolsServerApiClient.RemoveGitIgnoreFileTypeName(recordKey, cancellationToken);
            if (updateGitRepoByKeyResult.IsFailure)
            {
                updateGitRepoByKeyResult.Error.PrintErrorsOnConsole();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }
}
