using Microsoft.Extensions.Logging;
using SupportToolsData;

namespace LibAppProjectCreator.Models;

public sealed class AppProjectCreatorData
{
    private AppProjectCreatorData(string projectName, string? projectShortName, string? dbPartProjectName,
        ESupportProjectType projectType,
        string solutionFolderName, string workFolderPath, string securityWorkFolderPath, int indentSize)
    {
        ProjectName = projectName;
        ProjectShortName = projectShortName;
        DbPartProjectName = dbPartProjectName;
        ProjectType = projectType;
        SolutionFolderName = solutionFolderName;
        WorkFolderPath = workFolderPath;
        SecurityWorkFolderPath = securityWorkFolderPath;
        IndentSize = indentSize;
    }

    public string ProjectName { get; }
    public string? ProjectShortName { get; }
    public string? DbPartProjectName { get; }
    public ESupportProjectType ProjectType { get; }
    public string SolutionFolderName { get; }
    public string WorkFolderPath { get; }
    public string SecurityWorkFolderPath { get; }
    public int IndentSize { get; }

    public static AppProjectCreatorData? Create(ILogger logger, string projectName, string? projectShortName,
        string? dbPartProjectName,
        ESupportProjectType projectType, string? solutionFolderName, string? workFolderPath,
        string? securityWorkFolderPath, int indentSize)
    {
        if (string.IsNullOrWhiteSpace(solutionFolderName))
        {
            logger.LogError("solutionFolderName is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(workFolderPath))
        {
            logger.LogError("workFolderPath is empty");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(securityWorkFolderPath))
            return new AppProjectCreatorData(projectName, projectShortName, dbPartProjectName, projectType,
                solutionFolderName,
                workFolderPath, securityWorkFolderPath, indentSize);

        logger.LogError("securityWorkFolderPath is empty");
        return null;
    }
}