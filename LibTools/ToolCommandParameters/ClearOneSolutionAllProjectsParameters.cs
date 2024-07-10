//using LibGitData;
//using LibGitData.Models;
//using LibParameters;
//using Microsoft.Extensions.Logging;
//using SupportToolsData.Models;
//using System.Linq;
//using SystemToolsShared;

//namespace LibTools.ToolCommandParameters;

//public class ClearOneSolutionAllProjectsParameters : IParameters
//{
//    // ReSharper disable once ConvertToPrimaryConstructor
//    public ClearOneSolutionAllProjectsParameters(string? projectName)
//    {
//        ProjectName = projectName;
//    }

//    public string? ProjectName { get; }

//    public bool CheckBeforeSave()
//    {
//        return true;
//    }


//    public static ClearOneSolutionAllProjectsParameters? Create(ILogger? logger, SupportToolsParameters supportToolsParameters, string projectName, EGitCol gitCol, bool useConsole)
//    {
//        var project = supportToolsParameters.GetProject(projectName);

//        if (project == null)
//        {
//            StShared.WriteErrorLine("project is not found", true);
//            return null;
//        }

//        var gitsFolder = supportToolsParameters.GetGitsFolder(projectName, gitCol);

//        if (gitsFolder == null)
//        {
//            StShared.WriteErrorLine("Gits folder is not found", true);
//            return null;
//        }

//        var gitProjectNames = gitCol switch
//        {
//            EGitCol.Main => project.GitProjectNames,
//            EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
//            _ => null
//        } ?? [];

//        return new ClearOneSolutionAllProjectsParameters(projectName, gitsFolder);
//    }
//}