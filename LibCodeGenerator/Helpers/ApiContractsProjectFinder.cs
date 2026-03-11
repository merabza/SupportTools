using System;
using System.IO;
using System.Linq;

namespace LibCodeGenerator.Helpers;

public static class ApiContractsProjectFinder
{
    public static string? FindApiContractsProject(string projectFolder, string? projectApiContractsProjectName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || !Directory.Exists(projectFolder))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(projectApiContractsProjectName))
        {
            return null;
        }

        // Find all .csproj files recursively
        string[] csprojFiles = Directory.GetFiles(projectFolder, "*.csproj", SearchOption.AllDirectories);

        // Find the first .csproj file matching the projectApiContractsProjectName
        string? targetCsproj = csprojFiles.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f)
                .Equals(projectApiContractsProjectName, StringComparison.OrdinalIgnoreCase));

        return targetCsproj;

        //// Assume ApiRoutes class file is named "ApiRoutes.cs" and is in the same folder or subfolder of the project
        //var projectDir = Path.GetDirectoryName(targetCsproj);
        //if (projectDir is null)
        //    return null;

        //var apiRoutesFile = Directory.GetFiles(projectDir, "ApiRoutes.cs", SearchOption.AllDirectories)
        //    .FirstOrDefault();

        //return apiRoutesFile;
    }
}
