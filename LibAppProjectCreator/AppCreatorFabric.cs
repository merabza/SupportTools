﻿using LibAppProjectCreator.AppCreators;
using LibAppProjectCreator.Models;
using LibGitData.Models;
using LibGitWork;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using System;

namespace LibAppProjectCreator;

public static class AppCreatorFabric
{
    public static AppCreatorBase? CreateAppCreator(ILogger logger, AppProjectCreatorData par, TemplateModel template,
        GitProjects gitProjects, GitRepos gitRepos)
    {
        var appCreatorBaseData = AppCreatorBaseData.Create(logger, par.WorkFolderPath, par.ProjectName,
            par.SolutionFolderName, par.SecurityWorkFolderPath);

        if (appCreatorBaseData is null)
        {
            logger.LogError("appCreatorBaseData does not created");
            return null;
        }

        switch (par.ProjectType)
        {
            case ESupportProjectType.Console:
                var consoleAppWithDatabaseCreatorData =
                    ConsoleAppCreatorData.Create(appCreatorBaseData, par.ProjectName, template);
                return new ConsoleAppCreator(logger, par.ProjectName, par.IndentSize, gitProjects, gitRepos,
                    consoleAppWithDatabaseCreatorData);
            case ESupportProjectType.Api:
                var apiAppCreatorData =
                    ApiAppCreatorData.CreateApiAppCreatorData(logger, appCreatorBaseData, par.ProjectName, template);
                if (par.ProjectShortName is null)
                {
                    logger.LogError("ProjectShortName is not specified");
                    return null;
                }

                if (apiAppCreatorData is not null)
                    return new ApiAppCreator(logger, par.ProjectShortName, par.ProjectName, par.IndentSize, gitProjects,
                        gitRepos, apiAppCreatorData);
                logger.LogError("apiAppCreatorData is not created");
                return null;

            case ESupportProjectType.ScaffoldSeeder:
                return null;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}