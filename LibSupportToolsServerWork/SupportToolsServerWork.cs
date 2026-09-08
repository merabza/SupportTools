using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibSupportToolsServerWork;

public static class SupportToolsServerWork
{
    public static List<StsGitDataModel> GetGitRepos(ILogger logger, IHttpClientFactory httpClientFactory,
        SupportToolsParameters supportToolsParameters)
    {
        try
        {
            SupportToolsServerApiClient? supportToolsServerApiClient =
                supportToolsParameters.GetSupportToolsServerApiClient(logger, httpClientFactory);

            if (supportToolsServerApiClient is null)
            {
                StShared.WriteErrorLine("supportToolsServerApiClient is null", true, logger);
                return [];
            }

            Result<List<StsGitDataModel>> remoteGitReposResult = supportToolsServerApiClient.GetGitRepos().Result;
            if (remoteGitReposResult.IsSuccess)
            {
                return remoteGitReposResult.Value;
            }

            StShared.WriteErrorLine("could not received remoteGits", true, logger);
            remoteGitReposResult.Error.PrintErrorsOnConsole();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            //throw;
        }

        return [];
    }
}
