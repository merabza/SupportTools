using Microsoft.Extensions.Logging;
using SupportToolsData;

namespace LibAppProjectCreator.Models;

public sealed class AppProjectCreatorData
{
    private AppProjectCreatorData(string projectName, string projectShortName, ESupportProjectType projectType,
        string solutionFolderName, string workFolderPath, string securityWorkFolderPath, string logsFolderPath,
        string? tempFolderPath, int indentSize)
    {
        ProjectName = projectName;
        ProjectShortName = projectShortName;
        ProjectType = projectType;
        SolutionFolderName = solutionFolderName;
        WorkFolderPath = workFolderPath;
        SecurityWorkFolderPath = securityWorkFolderPath;
        LogsFolderPath = logsFolderPath;
        TempFolderPath = tempFolderPath;
        IndentSize = indentSize;
    }

    public string ProjectName { get; }
    public string ProjectShortName { get; }
    public ESupportProjectType ProjectType { get; }
    public string SolutionFolderName { get; }
    public string WorkFolderPath { get; }
    public string SecurityWorkFolderPath { get; }
    public string LogsFolderPath { get; }
    public string? TempFolderPath { get; }
    public int IndentSize { get; }

    public static AppProjectCreatorData? Create(ILogger logger, string projectName, string projectShortName,
        ESupportProjectType projectType, string? solutionFolderName, string? workFolderPath,
        string? securityWorkFolderPath, string? logsFolderPath, string? tempFolderPath, int indentSize)
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

        if (string.IsNullOrWhiteSpace(securityWorkFolderPath))
        {
            logger.LogError("securityWorkFolderPath is empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(logsFolderPath))
        {
            logger.LogError("logsFolderPath is empty");
            return null;
        }

        return new AppProjectCreatorData(projectName, projectShortName, projectType, solutionFolderName, workFolderPath,
            securityWorkFolderPath, logsFolderPath, tempFolderPath, indentSize);
    }
}