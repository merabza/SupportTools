using System.IO;
using LibGitData.Models;
using SupportToolsData.Models;

namespace SupportTools.Tests.Menu.ReversePackageDistribution;

//the projects of the Reverse Package Distribution tests: a local project {temp}\{name} with its main git {name}, whose
//{name}\{name}.Api\{name}.Api.csproj uses the own package Lib.A; the project file of Lib.A,
//{temp}\{name}\Lib\Lib.A\Lib.A.csproj, exists
internal static class ReversePackageDistributionTestProjects
{
    //an own package is one that GitProjects has
    public static void AddOwnPackageLibA(SupportToolsParameters parameters)
    {
        parameters.GitProjects["Lib.A"] = new GitProjectDataModel
        {
            GitName = "Lib", ProjectRelativePath = @"Lib\Lib.A", ProjectFileName = "Lib.A.csproj"
        };
    }

    //returns the csproj that uses Lib.A
    public static string AddProjectWithOwnPackage(SupportToolsParameters parameters, string tempFolder,
        string projectName, string? projectGroupName = null)
    {
        string projectFolder = Path.Combine(tempFolder, projectName);
        string apiFolder = Path.Combine(projectFolder, projectName, $"{projectName}.Api");
        Directory.CreateDirectory(apiFolder);
        string csprojFile = Path.Combine(apiFolder, $"{projectName}.Api.csproj");
        File.WriteAllText(csprojFile,
            """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Lib.A" /></ItemGroup></Project>""");
        string libAProjectFile = GetLibAProjectFile(tempFolder, projectName);
        Directory.CreateDirectory(Path.GetDirectoryName(libAProjectFile)!);
        File.WriteAllText(libAProjectFile, "<Project />");
        parameters.Projects[projectName] = new ProjectModel
        {
            UseLocalPackages = true,
            ProjectGroupName = projectGroupName,
            ProjectFolderName = projectFolder,
            GitProjectNames = [projectName]
        };
        parameters.Gits[projectName] = new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{projectName}.git",
            GitProjectFolderName = projectName,
            GitIgnorePatternName = "CSharp"
        };
        return csprojFile;
    }

    public static string GetLibAProjectFile(string tempFolder, string projectName)
    {
        return Path.Combine(tempFolder, projectName, "Lib", "Lib.A", "Lib.A.csproj");
    }

    //the editor calls that replace Lib.A of the project with its project reference
    public static string[] GetReplaceCalls(string csprojFile, string tempFolder, string projectName)
    {
        return [$"remove {csprojFile} Lib.A", $"reference {csprojFile} {GetLibAProjectFile(tempFolder, projectName)}"];
    }
}
