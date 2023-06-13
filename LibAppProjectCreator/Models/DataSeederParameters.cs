using System;
using System.IO;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class JetBrainsCleanupCodeRunnerParameters : IParameters
{
    public JetBrainsCleanupCodeRunnerParameters(string solutionFileName)
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
            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (string.IsNullOrWhiteSpace(project.SolutionFileName))
            {
                StShared.WriteErrorLine($"SolutionFileName does not specified for project {projectName}", true);
                return null;
            }

            if (!File.Exists(project.SolutionFileName))
            {
                StShared.WriteErrorLine(
                    $"SolutionFile with name {project.SolutionFileName} does not Exists. Project {projectName}", true);
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