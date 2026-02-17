using System;
using System.IO;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class JetBrainsCleanupCodeRunnerParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private JetBrainsCleanupCodeRunnerParameters(string solutionFileName)
    {
        SolutionFileName = solutionFileName;
    }

    public string SolutionFileName { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static JetBrainsCleanupCodeRunnerParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName)
    {
        try
        {
            ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

            if (string.IsNullOrWhiteSpace(project.SolutionFileName))
            {
                StShared.WriteErrorLine($"SolutionFileName does not specified for project {projectName}", true);
                return null;
            }

            if (!File.Exists(project.SolutionFileName))
            {
                StShared.WriteErrorLine(
                    $"SolutionFile with name {project.SolutionFileName} is not Exists. Project {projectName}", true);
                return null;
            }

            var dataSeederParameters = new JetBrainsCleanupCodeRunnerParameters(project.SolutionFileName);
            return dataSeederParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}
