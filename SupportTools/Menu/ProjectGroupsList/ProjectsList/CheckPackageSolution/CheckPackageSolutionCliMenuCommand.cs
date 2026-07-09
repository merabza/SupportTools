using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using AppCliTools.CliMenu;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.ApiClientsManagement;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;

public sealed class CheckPackageSolutionCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check Package Solution";
    private const string BaGetterSourceKey = "BaGetter";

    private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckPackageSolutionCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName) :
        base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return ValueTask.FromResult(false);
        }

        //ეს ბრძანება მუშაობს მხოლოდ პაკეტის ტიპის პროექტებისთვის
        if (project.ProjectType != EProjectType.IsPackage)
        {
            StShared.WriteErrorLine($"Project {_projectName} is not package type project", true);
            return ValueTask.FromResult(false);
        }

        string? localPackageManagerWebApiClientName = parameters.LocalPackageManagerWebApiClientName;
        if (string.IsNullOrWhiteSpace(localPackageManagerWebApiClientName))
        {
            StShared.WriteErrorLine("LocalPackageManagerWebApiClientName does not specified", true);
            return ValueTask.FromResult(false);
        }

        ApiClientSettingsDomain apiClientSettings;
        try
        {
            apiClientSettings = parameters.GetApiClientSettingsRequired(localPackageManagerWebApiClientName);
        }
        catch (InvalidOperationException e)
        {
            StShared.WriteException(e, true, _logger);
            return ValueTask.FromResult(false);
        }

        string? solutionFileName = project.SolutionFileName;
        if (string.IsNullOrWhiteSpace(solutionFileName))
        {
            StShared.WriteErrorLine($"Solution file name does not specified for {_projectName}", true);
            return ValueTask.FromResult(false);
        }

        string? solutionFolder = Path.GetDirectoryName(solutionFileName);
        if (string.IsNullOrWhiteSpace(solutionFolder))
        {
            StShared.WriteErrorLine($"Cannot get solution folder for {solutionFileName}", true);
            return ValueTask.FromResult(false);
        }

        string solutionName = Path.GetFileNameWithoutExtension(solutionFileName);

        //სოლუშენში შემავალი პროექტების ჩამონათვალის მიღება
        var dotnetProcessor = new DotnetProcessor(_logger, true);
        OneOf<List<string>, Error[]> projectsListResult = dotnetProcessor.GetSolutionProjectsList(solutionFileName);
        if (projectsListResult.IsT1)
        {
            StShared.WriteErrorLine($"Cannot get projects list for solution {solutionFileName}", true, _logger);
            return ValueTask.FromResult(false);
        }

        List<string> solutionProjectFullPaths = projectsListResult.AsT0
            .Select(relativePath => Path.GetFullPath(Path.Combine(solutionFolder, relativePath))).ToList();

        //სოლუშენის ფოლდერში ისეთი პროექტების მოძებნა, რომლებიც სოლუშენში არ არის გაერთიანებული
        if (!CheckNoOrphanProjects(solutionName, solutionFolder, solutionProjectFullPaths))
        {
            return ValueTask.FromResult(false);
        }

        //სოლუშენში შემავალი ყველა პროექტის სახელი უნდა იწყებოდეს სოლუშენის სახელით და წერტილით
        if (!CheckProjectNamePrefixes(solutionName, solutionProjectFullPaths))
        {
            return ValueTask.FromResult(false);
        }

        //გლობალურ NuGet კონფიგში packageSourceMapping ჩანაწერის შემოწმება და საჭიროების შემთხვევაში დამატება
        string serviceIndexUrl = NugetServerUrls.GetServiceIndexUrl(apiClientSettings.Server);
        if (!EnsureNugetConfigPackageSourceMapping(solutionName, serviceIndexUrl))
        {
            return ValueTask.FromResult(false);
        }

        Console.WriteLine($"Solution {solutionName} check passed");
        return ValueTask.FromResult(true);
    }

    private static bool CheckNoOrphanProjects(string solutionName, string solutionFolder,
        List<string> solutionProjectFullPaths)
    {
        var solutionProjectsSet = new HashSet<string>(solutionProjectFullPaths, StringComparer.OrdinalIgnoreCase);

        List<string> orphanProjects = Directory.GetFiles(solutionFolder, "*.csproj", SearchOption.AllDirectories)
            .Where(projectFileName => !IsInBinOrObjFolder(projectFileName))
            .Select(projectFileName => Path.GetFullPath(projectFileName))
            .Where(projectFileName => !solutionProjectsSet.Contains(projectFileName)).ToList();

        if (orphanProjects.Count == 0)
        {
            return true;
        }

        StShared.WriteErrorLine(
            $"Solution {solutionName} folder contains projects that are not included in the solution: {string.Join(", ", orphanProjects)}",
            true);
        return false;
    }

    private static bool IsInBinOrObjFolder(string fileFullPath)
    {
        return fileFullPath.Split(DirectorySeparators).Any(pathPart =>
            pathPart.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            pathPart.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static bool CheckProjectNamePrefixes(string solutionName, List<string> solutionProjectFullPaths)
    {
        List<string> invalidProjectNames = solutionProjectFullPaths
            .Select(projectFileName => Path.GetFileNameWithoutExtension(projectFileName)).Where(projectName =>
                !projectName.StartsWith(solutionName + ".", StringComparison.Ordinal)).ToList();

        if (invalidProjectNames.Count == 0)
        {
            return true;
        }

        StShared.WriteErrorLine(
            $"Projects in solution {solutionName} must have names starting with \"{solutionName}.\": {string.Join(", ", invalidProjectNames)}",
            true);
        return false;
    }

    private bool EnsureNugetConfigPackageSourceMapping(string solutionName, string serviceIndexUrl)
    {
        string nugetConfigFolderPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet");
        string nugetConfigFilePath = Path.Combine(nugetConfigFolderPath, "NuGet.Config");
        string packagePattern = solutionName + ".*";

        try
        {
            XDocument nugetConfigDocument;
            if (File.Exists(nugetConfigFilePath))
            {
                nugetConfigDocument = XDocument.Load(nugetConfigFilePath);
            }
            else
            {
                if (!StShared.CreateFolder(nugetConfigFolderPath, true))
                {
                    return false;
                }

                nugetConfigDocument = new XDocument(new XElement("configuration"));
            }

            XElement? configurationElement = nugetConfigDocument.Root;
            if (configurationElement is null || configurationElement.Name.LocalName != "configuration")
            {
                StShared.WriteErrorLine($"Invalid NuGet config file {nugetConfigFilePath}", true);
                return false;
            }

            bool modified = false;

            //BaGetter პაკეტების წყაროს შემოწმება და საჭიროების შემთხვევაში დამატება
            XElement? packageSourcesElement = configurationElement.Element("packageSources");
            if (packageSourcesElement is null)
            {
                packageSourcesElement = new XElement("packageSources");
                configurationElement.Add(packageSourcesElement);
            }

            XElement? baGetterSourceElement = packageSourcesElement.Elements("add").FirstOrDefault(addElement =>
                string.Equals((string?)addElement.Attribute("key"), BaGetterSourceKey,
                    StringComparison.OrdinalIgnoreCase));
            if (baGetterSourceElement is null)
            {
                packageSourcesElement.Add(new XElement("add", new XAttribute("key", BaGetterSourceKey),
                    new XAttribute("value", serviceIndexUrl)));
                modified = true;
                Console.WriteLine($"Package source {BaGetterSourceKey} added to {nugetConfigFilePath}");
            }

            //packageSourceMapping სექციაში BaGetter წყაროსთვის {სოლუშენის სახელი}.* შაბლონის შემოწმება
            XElement? packageSourceMappingElement = configurationElement.Element("packageSourceMapping");
            if (packageSourceMappingElement is null)
            {
                packageSourceMappingElement = new XElement("packageSourceMapping");
                configurationElement.Add(packageSourceMappingElement);
            }

            XElement? baGetterMappingElement = packageSourceMappingElement.Elements("packageSource")
                .FirstOrDefault(packageSourceElement => string.Equals((string?)packageSourceElement.Attribute("key"),
                    BaGetterSourceKey, StringComparison.OrdinalIgnoreCase));
            if (baGetterMappingElement is null)
            {
                baGetterMappingElement = new XElement("packageSource", new XAttribute("key", BaGetterSourceKey));
                packageSourceMappingElement.Add(baGetterMappingElement);
            }

            if (!baGetterMappingElement.Elements("package").Any(packageElement =>
                    string.Equals((string?)packageElement.Attribute("pattern"), packagePattern,
                        StringComparison.OrdinalIgnoreCase)))
            {
                baGetterMappingElement.Add(new XElement("package", new XAttribute("pattern", packagePattern)));
                modified = true;
                Console.WriteLine(
                    $"Package pattern {packagePattern} added for {BaGetterSourceKey} in {nugetConfigFilePath}");
            }

            if (modified)
            {
                nugetConfigDocument.Save(nugetConfigFilePath);
            }

            return true;
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            StShared.WriteException(e, true, _logger);
            return false;
        }
    }
}
