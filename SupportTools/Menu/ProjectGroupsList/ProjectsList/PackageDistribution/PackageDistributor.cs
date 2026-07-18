using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibDotnetWork;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.ApiClientsManagement;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//პაკეტის მომხმარებელ პროექტებში გავრცელების ლოგიკა. გამოიყენება როგორც ცალკე მენიუს ბრძანებიდან,
//ისე ყველა პაკეტის გავრცელების პროცესიდან
public sealed class PackageDistributor
{
    private readonly IHttpClientFactory _httpClientFactory;

    //ერთხელ მოთხოვნილი ვერსიები ინახება ქეშში, რომ ერთი და იგივე პაკეტისთვის სერვერს რამდენჯერმე არ მივმართოთ
    private readonly Dictionary<string, string> _latestVersionsCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    //flat container-ის საბაზისო მისამართი, რომელიც ერთხელ იკითხება სერვერის service index-იდან
    private string? _packageBaseAddress;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PackageDistributor(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string projectName)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    public async Task<bool> DistributePackage(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return false;
        }

        //პაკეტის გავრცელება მუშაობს მხოლოდ პაკეტის ტიპის პროექტებისთვის
        if (project.ProjectType != EProjectType.IsPackage)
        {
            StShared.WriteErrorLine($"Project {_projectName} is not package type project", true);
            return false;
        }

        //პაკეტის პროექტის შესაბამისი git რეპოზიტორია არის ის, რომლის სახელიც ემთხვევა პროექტის სახელს
        if (!parameters.Gits.ContainsKey(_projectName))
        {
            StShared.WriteErrorLine($"Git repository with name {_projectName} does not exists in Gits list", true);
            return false;
        }

        string? localPackageManagerWebApiClientName = parameters.LocalPackageManagerWebApiClientName;
        if (string.IsNullOrWhiteSpace(localPackageManagerWebApiClientName))
        {
            StShared.WriteErrorLine("LocalPackageManagerWebApiClientName does not specified", true);
            return false;
        }

        ApiClientSettingsDomain apiClientSettings;
        try
        {
            apiClientSettings = parameters.GetApiClientSettingsRequired(localPackageManagerWebApiClientName);
        }
        catch (InvalidOperationException e)
        {
            StShared.WriteException(e, true, _logger);
            return false;
        }

        var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);

        bool hadErrors = false;
        int consumersCount = 0;

        //მომხმარებელი პროექტია ყველა ის პროექტი, რომლის გიტების სიაშიც არის პაკეტის პროექტის რეპოზიტორია
        foreach ((string consumerProjectName, ProjectModel consumerProject) in parameters.Projects.OrderBy(x => x.Key,
                     StringComparer.Ordinal))
        {
            if (consumerProjectName == _projectName)
            {
                continue;
            }

            if (!consumerProject.GitProjectNames.Contains(_projectName))
            {
                continue;
            }

            consumersCount++;
            hadErrors |= !await ProcessConsumerProject(parameters, gitProjects, consumerProjectName, consumerProject,
                apiClientSettings.Server, cancellationToken);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Package distribution for {ProjectName} finished. Consumer projects processed: {ConsumersCount}",
                _projectName, consumersCount);
        }

        return !hadErrors;
    }

    //ერთი მომხმარებელი პროექტის დამუშავება. ცვლილებები შედის მხოლოდ მთავარ რეპოზიტორიაში
    private async Task<bool> ProcessConsumerProject(SupportToolsParameters parameters, GitProjects gitProjects,
        string consumerProjectName, ProjectModel consumerProject, string server, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(consumerProject.ProjectFolderName))
        {
            StShared.WriteWarningLine(
                $"ProjectFolderName does not specified for project {consumerProjectName}, skipped", true, _logger);
            return true;
        }

        var gitRepos = GitRepos.Create(_logger, parameters.Gits,
            consumerProject.SpaProjectFolderRelativePath(gitProjects), true, false);

        //მომხმარებელი პროექტის მთავარი რეპოზიტორია არის ის, რომლის სახელიც ემთხვევა პროექტის სახელს
        if (!consumerProject.GitProjectNames.Contains(consumerProjectName) ||
            !gitRepos.Gits.TryGetValue(consumerProjectName, out GitData? mainGitData))
        {
            StShared.WriteWarningLine($"Main git repository does not found for project {consumerProjectName}, skipped",
                true, _logger);
            return true;
        }

        if (!gitRepos.Gits.TryGetValue(_projectName, out GitData? packageGitData))
        {
            StShared.WriteWarningLine(
                $"Git repository {_projectName} does not found in gits list for project {consumerProjectName}, skipped",
                true, _logger);
            return true;
        }

        string mainRepoPath =
            Path.GetFullPath(Path.Combine(consumerProject.ProjectFolderName, mainGitData.GitProjectFolderName));
        string packageRepoPath =
            Path.GetFullPath(Path.Combine(consumerProject.ProjectFolderName, packageGitData.GitProjectFolderName));

        if (!Directory.Exists(mainRepoPath))
        {
            StShared.WriteWarningLine(
                $"Main repository folder {mainRepoPath} does not exists for project {consumerProjectName}, skipped",
                true, _logger);
            return true;
        }

        string packageRepoPrefix = packageRepoPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        var dotnetProcessor = new DotnetProcessor(_logger, true);

        //მანამდე უკვე ჩასმული ამავე პაკეტის პროექტის პაკეტების ვერსიები უახლესზე აიწევა,
        //რომ ძველ ვერსიებზე მიბმის გამო ვერსიების კონფლიქტები (NU1107) არ წარმოიშვას
        bool hadErrors = !await UpdateExistingPackageVersions(mainRepoPath, packageRepoPath, server, cancellationToken);

        foreach (string csprojFile in Directory.EnumerateFiles(mainRepoPath, "*.csproj", SearchOption.AllDirectories))
        {
            string? csprojFolder = Path.GetDirectoryName(csprojFile);
            if (csprojFolder is null)
            {
                continue;
            }

            StShared.ConsoleWriteInformationLine(_logger, true, $"Processing {csprojFile}");
            //ჯერ მხოლოდ იკითხება csproj ფაილი, ცვლილებები კეთდება dotnet-ის ბრძანებებით
            XElement projectXml = XElement.Load(csprojFile);
            List<string> includes = projectXml.Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include")).OfType<string>().ToList();

            foreach (string include in includes)
            {
                string refFullPath = Path.GetFullPath(Path.Combine(csprojFolder, include));

                //გვაინტერესებს მხოლოდ ის რეფერენსები, რომლებიც პაკეტის პროექტის რეპოზიტორიაში შედიან
                if (!refFullPath.StartsWith(packageRepoPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                //პაკეტის სახელი ემთხვევა csproj ფაილის სახელს
                string packageId = Path.GetFileNameWithoutExtension(refFullPath);

                //ჯერ ვერსიის მოძიება, რომ რეფერენსი არ წაიშალოს, თუ ჩამნაცვლებელი პაკეტი ვერ მოიძებნება
                string? latestVersion = await GetLatestPackageVersion(server, packageId, cancellationToken);
                if (latestVersion is null)
                {
                    hadErrors = true;
                    continue;
                }

                if (dotnetProcessor.RemoveReferenceFromProject(csprojFile, refFullPath).IsSome)
                {
                    StShared.WriteErrorLine($"Cannot remove reference {refFullPath} from {csprojFile}", true, _logger);
                    hadErrors = true;
                    continue;
                }

                if (dotnetProcessor.AddPackageToProject(csprojFile, packageId, latestVersion).IsSome)
                {
                    hadErrors = true;

                    //თუ პაკეტის ჩასმა ვერ მოხერხდა, წაშლილი რეფერენსი უბრუნდება პროექტს,
                    //რომ შეცვლილი პროექტი შეცდომაზე არ გავიდეს
                    if (dotnetProcessor.AddReferenceToProject(csprojFile, refFullPath).IsSome)
                    {
                        StShared.WriteErrorLine(
                            $"Cannot add package {packageId} to {csprojFile} and cannot restore removed reference {refFullPath}. Recover the file from git",
                            true, _logger);
                        continue;
                    }

                    StShared.WriteErrorLine(
                        $"Cannot add package {packageId} to {csprojFile}. Removed reference was restored", true,
                        _logger);
                    continue;
                }

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Replaced ProjectReference {ReferencePath} with package {PackageId} {PackageVersion} in {ProjectFilePath}",
                        refFullPath, packageId, latestVersion, csprojFile);
                }
            }
        }

        return !hadErrors;
    }

    //მომხმარებელი პროექტის მთავარ რეპოზიტორიაში უკვე არსებული ამ პაკეტის პროექტის პაკეტების
    //ვერსიების აწევა უახლესზე Directory.Packages.props ფაილებში
    private async Task<bool> UpdateExistingPackageVersions(string mainRepoPath, string packageRepoPath, string server,
        CancellationToken cancellationToken)
    {
        //პაკეტის პროექტის პაკეტების სახელები ემთხვევა მისი რეპოზიტორიის csproj ფაილების სახელებს
        HashSet<string>? packageIds = null;
        if (Directory.Exists(packageRepoPath))
        {
            packageIds = Directory.EnumerateFiles(packageRepoPath, "*.csproj", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        bool hadErrors = false;

        foreach (string propsFile in Directory.EnumerateFiles(mainRepoPath, "Directory.Packages.props",
                     SearchOption.AllDirectories))
        {
            XDocument propsXml = XDocument.Load(propsFile, LoadOptions.PreserveWhitespace);
            bool changed = false;

            foreach (XElement packageVersionElement in propsXml.Descendants("PackageVersion"))
            {
                string? packageId = (string?)packageVersionElement.Attribute("Include");
                if (string.IsNullOrWhiteSpace(packageId))
                {
                    continue;
                }

                //თუ პაკეტის რეპოზიტორიის კლონი ვერ მოიძებნა, გამოიყენება სახელების კონვენცია
                bool isPackageProjectPackage = packageIds?.Contains(packageId) ??
                                               packageId.Equals(_projectName, StringComparison.OrdinalIgnoreCase) ||
                                               packageId.StartsWith(_projectName + ".",
                                                   StringComparison.OrdinalIgnoreCase);
                if (!isPackageProjectPackage)
                {
                    continue;
                }

                string? latestVersion = await GetLatestPackageVersion(server, packageId, cancellationToken);
                if (latestVersion is null)
                {
                    hadErrors = true;
                    continue;
                }

                string? currentVersion = (string?)packageVersionElement.Attribute("Version");
                if (string.Equals(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                packageVersionElement.SetAttributeValue("Version", latestVersion);
                changed = true;

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Updated package {PackageId} version {CurrentVersion} -> {LatestVersion} in {PropsFile}",
                        packageId, currentVersion, latestVersion, propsFile);
                }
            }

            if (changed)
            {
                propsXml.Save(propsFile, SaveOptions.DisableFormatting);
            }
        }

        return !hadErrors;
    }

    //flat container-ის საბაზისო მისამართის დადგენა სერვერის service index-იდან.
    //სხვადასხვა სერვერს ეს მისამართი სხვადასხვა აქვს (მაგალითად BaGetter-ს /v3/package/, nuget.org-ს /v3-flatcontainer/),
    //ამიტომ მისი აღმოჩენა ხდება PackageBaseAddress რესურსით
    private async Task<string?> GetPackageBaseAddress(string server, CancellationToken cancellationToken)
    {
        if (_packageBaseAddress is not null)
        {
            return _packageBaseAddress;
        }

        var serviceIndexUri = new Uri(NugetServerUrls.GetServiceIndexUrl(server));

        // ReSharper disable once using
        using HttpClient client = _httpClientFactory.CreateClient();
        // ReSharper disable once using
        using HttpResponseMessage response = await client.GetAsync(serviceIndexUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            StShared.WriteErrorLine(
                $"Cannot get nuget service index {serviceIndexUri}. Status code is {response.StatusCode}", true,
                _logger);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        string? packageBaseAddress = JObject.Parse(content)["resources"]
            ?.FirstOrDefault(x => (string?)x["@type"] == "PackageBaseAddress/3.0.0")?["@id"]?.ToString();

        if (string.IsNullOrWhiteSpace(packageBaseAddress))
        {
            StShared.WriteErrorLine(
                $"PackageBaseAddress resource does not found in nuget service index {serviceIndexUri}", true, _logger);
            return null;
        }

        _packageBaseAddress = packageBaseAddress.TrimEnd('/');
        return _packageBaseAddress;
    }

    //პაკეტის უახლესი ვერსიის მოძიება ლოკალურ პაკეტების სერვერზე flat container-ის საშუალებით
    private async Task<string?> GetLatestPackageVersion(string server, string packageId,
        CancellationToken cancellationToken)
    {
        if (_latestVersionsCache.TryGetValue(packageId, out string? cachedVersion))
        {
            return cachedVersion;
        }

        string? packageBaseAddress = await GetPackageBaseAddress(server, cancellationToken);
        if (packageBaseAddress is null)
        {
            return null;
        }

        var uri = new Uri($"{packageBaseAddress}/{packageId.ToLowerInvariant()}/index.json");

        // ReSharper disable once using
        using HttpClient client = _httpClientFactory.CreateClient();
        // ReSharper disable once using
        using HttpResponseMessage response = await client.GetAsync(uri, cancellationToken);

        //404 ნიშნავს, რომ პაკეტი სერვერზე ჯერ არ ატვირთულა
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            StShared.WriteErrorLine(
                $"Package {packageId} does not found on local package manager. Publish it first with {BuildPackageCliMenuCommand.MenuCommandName} command",
                true, _logger);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            StShared.WriteErrorLine(
                $"Cannot get versions for package {packageId}. Status code is {response.StatusCode}", true, _logger);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        var versions = JObject.Parse(content)["versions"]?.ToObject<List<string>>();
        if (versions is null || versions.Count == 0)
        {
            StShared.WriteErrorLine($"Versions list is empty for package {packageId}", true, _logger);
            return null;
        }

        string latestVersion = GetLatestVersion(versions);
        _latestVersionsCache.Add(packageId, latestVersion);
        return latestVersion;
    }

    //ამ სისტემის მიერ შექმნილი ვერსიები 4-ნაწილიანი რიცხვითი ვერსიებია, ამიტომ მაქსიმალურის ასარჩევად გამოიყენება Version.
    //თუ რომელიმე ვერსია ამ ფორმატს არ ემთხვევა, გამოიყენება სიის ბოლო ელემენტი (სერვერი ვერსიებს ზრდადობით აბრუნებს)
    private static string GetLatestVersion(List<string> versions)
    {
        var parsedVersions = new List<(Version Parsed, string Original)>();
        foreach (string version in versions)
        {
            if (!Version.TryParse(version, out Version? parsed))
            {
                return versions[^1];
            }

            parsedVersions.Add((parsed, version));
        }

        return parsedVersions.MaxBy(x => x.Parsed).Original;
    }
}
