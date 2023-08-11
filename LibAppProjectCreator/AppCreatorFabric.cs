using System;
using System.Collections.Generic;
using LibAppProjectCreator.AppCreators;
using LibAppProjectCreator.Models;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;

namespace LibAppProjectCreator;

public static class AppCreatorFabric
{
    public static AppCreatorBase? CreateAppCreator(ILogger logger, AppProjectCreatorData par, TemplateModel template,
        GitProjects gitProjects, GitRepos gitRepos, bool forTest, string workFolder,
        Dictionary<string, string> reactAppTemplates)
    {
        var appCreatorBaseData = AppCreatorBaseData.Create(logger, par, forTest);

        if (appCreatorBaseData is null)
        {
            //logger.LogError("appCreatorBaseData does not created for project {projectName}", projectName);
            logger.LogError("appCreatorBaseData does not created");
            return null;
        }

        switch (par.ProjectType)
        {
            case ESupportProjectType.Console:
                var consoleAppWithDatabaseCreatorData = ConsoleAppCreatorData.Create(appCreatorBaseData, par, template);
                return new ConsoleAppCreator(logger, par, gitProjects, gitRepos, consoleAppWithDatabaseCreatorData);
            case ESupportProjectType.Api:
                var apiAppCreatorData =
                    ApiAppCreatorData.CreateApiAppCreatorData(logger, appCreatorBaseData, par, template);
                if (apiAppCreatorData is not null)
                    return new ApiAppCreator(logger, par, gitProjects, gitRepos, apiAppCreatorData, workFolder,
                        reactAppTemplates);
                logger.LogError("apiAppCreatorData does not created");
                return null;

            case ESupportProjectType.ScaffoldSeeder:
                return null;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}