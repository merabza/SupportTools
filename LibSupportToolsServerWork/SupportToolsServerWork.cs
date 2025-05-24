using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace LibSupportToolsServerWork;

public static class SupportToolsServerWork
{
    public static List<GitDataDto> GetGitRepos(ILogger logger, IHttpClientFactory httpClientFactory,
        SupportToolsParameters supportToolsParameters)
    {
        try
        {
            var supportToolsServerApiClient =
                supportToolsParameters.GetSupportToolsServerApiClient(logger, httpClientFactory);

            if (supportToolsServerApiClient is null)
            {
                StShared.WriteErrorLine("supportToolsServerApiClient is null", true, logger);
                return [];
            }

            var remoteGitReposResult = supportToolsServerApiClient.GetGitRepos().Result;
            if (remoteGitReposResult.IsT0) return remoteGitReposResult.AsT0;

            StShared.WriteErrorLine("could not received remoteGits", true, logger);
            Err.PrintErrorsOnConsole(remoteGitReposResult.AsT1);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            //throw;
        }

        return [];
    }
}