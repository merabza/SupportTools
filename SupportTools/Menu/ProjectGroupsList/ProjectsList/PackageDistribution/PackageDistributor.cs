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
using LibGitWork.Helpers;
using LibGitWork.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.ApiClientsManagement;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//პაკეტის მომხმარებელ პროექტებში გავრცელების ლოგიკა. გამოიყენება როგორც ცალკე მენიუს ბრძანებიდან (ერთი პაკეტი ყველა
//მომხმარებელ პროექტში), ისე ყველა პაკეტის გავრცელების პროცესიდან (ერთი პროექტი ამ გაშვების რამდენიმე პაკეტს იღებს)
public sealed class PackageDistributor
{
    private readonly IHttpClientFactory _httpClientFactory;

    //ერთხელ მოთხოვნილი ვერსიები ინახება ქეშში, რომ ერთი და იგივე პაკეტისთვის სერვერს რამდენჯერმე არ მივმართოთ
    private readonly Dictionary<string, string> _latestVersionsCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly ProjectReferenceReplacer _referenceReplacer;

    //flat container-ის საბაზისო მისამართი, რომელიც ერთხელ იკითხება სერვერის service index-იდან
    private string? _packageBaseAddress;

    public PackageDistributor(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        new DotnetPackageReferenceCommands(new DotnetProcessor(logger, true)))
    {
    }

    //csproj ფაილების ცვლილებების ბრძანებები პარამეტრადაა გამოტანილი, რომ ტესტებმა dotnet არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal PackageDistributor(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, IPackageReferenceCommands referenceCommands)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _referenceReplacer = new ProjectReferenceReplacer(referenceCommands, logger);
    }

    //ერთი პაკეტის გავრცელება ყველა მომხმარებელ პროექტში
    public async Task<bool> DistributePackage(string packageProjectName, CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        string? server = GetPackageServer(parameters, [packageProjectName]);
        if (server is null)
        {
            return false;
        }

        var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);

        bool hadErrors = false;
        int consumersCount = 0;

        //მომხმარებელი პროექტია ყველა ის პროექტი, რომლის გიტების სიაშიც არის პაკეტის პროექტის რეპოზიტორია
        foreach ((string consumerProjectName, ProjectModel consumerProject) in parameters.Projects.OrderBy(x => x.Key,
                     StringComparer.Ordinal))
        {
            if (consumerProjectName == packageProjectName)
            {
                continue;
            }

            if (!consumerProject.GitProjectNames.Contains(packageProjectName))
            {
                continue;
            }

            //ლოკალურ პაკეტებს მხოლოდ UseLocalPackages-ჩართული პროექტები იღებენ
            if (!consumerProject.UseLocalPackages)
            {
                StShared.ConsoleWriteInformationLine(_logger, true,
                    $"UseLocalPackages is off for project {consumerProjectName}, skipped");
                continue;
            }

            consumersCount++;
            hadErrors |= !await ProcessConsumerProject(parameters, gitProjects, consumerProjectName, consumerProject,
                [packageProjectName], server, cancellationToken);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Package distribution for {ProjectName} finished. Consumer projects processed: {ConsumersCount}",
                packageProjectName, consumersCount);
        }

        return !hadErrors;
    }

    //ერთი პროექტი რამდენიმე პაკეტს ერთად იღებს. „Distribute all packages“ ამას მაშინ იძახებს, როცა ეს პაკეტები უკვე
    //დაფუშულია. packageProjectNames დამოკიდებულებების რიგითაა, რომ ყოველი პაკეტი თავის დამოკიდებულებებზე გვიან
    //გავრცელდეს
    public async Task<bool> DistributePackagesToProject(string projectName, IReadOnlyList<string> packageProjectNames,
        CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        string? server = GetPackageServer(parameters, packageProjectNames);
        if (server is null)
        {
            return false;
        }

        ProjectModel? project = parameters.GetProject(projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {projectName} does not found", true);
            return false;
        }

        var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);
        return await ProcessConsumerProject(parameters, gitProjects, projectName, project, packageProjectNames, server,
            cancellationToken);
    }

    //პაკეტების პროექტების შემოწმება და ლოკალური პაკეტების სერვერის მისამართი; null - გავრცელება შეუძლებელია
    private string? GetPackageServer(SupportToolsParameters parameters, IEnumerable<string> packageProjectNames)
    {
        foreach (string packageProjectName in packageProjectNames)
        {
            ProjectModel? project = parameters.GetProject(packageProjectName);
            if (project is null)
            {
                StShared.WriteErrorLine($"Project {packageProjectName} does not found", true);
                return null;
            }

            //პაკეტის გავრცელება მუშაობს მხოლოდ პაკეტის ტიპის პროექტებისთვის
            if (project.ProjectType != EProjectType.IsPackage)
            {
                StShared.WriteErrorLine($"Project {packageProjectName} is not package type project", true);
                return null;
            }

            //პაკეტის პროექტის შესაბამისი git რეპოზიტორია არის ის, რომლის სახელიც ემთხვევა პროექტის სახელს
            if (!parameters.Gits.ContainsKey(packageProjectName))
            {
                StShared.WriteErrorLine($"Git repository with name {packageProjectName} does not exists in Gits list",
                    true);
                return null;
            }
        }

        string? localPackageManagerWebApiClientName = parameters.LocalPackageManagerWebApiClientName;
        if (string.IsNullOrWhiteSpace(localPackageManagerWebApiClientName))
        {
            StShared.WriteErrorLine("LocalPackageManagerWebApiClientName does not specified", true);
            return null;
        }

        ApiClientSettingsDomain apiClientSettings;
        try
        {
            apiClientSettings = parameters.GetApiClientSettingsRequired(localPackageManagerWebApiClientName);
        }
        catch (InvalidOperationException e)
        {
            StShared.WriteException(e, true, _logger);
            return null;
        }

        return apiClientSettings.Server;
    }

    //ერთი მომხმარებელი პროექტის დამუშავება. ცვლილებები შედის მხოლოდ მთავარ რეპოზიტორიაში
    private async Task<bool> ProcessConsumerProject(SupportToolsParameters parameters, GitProjects gitProjects,
        string consumerProjectName, ProjectModel consumerProject, IReadOnlyList<string> packageProjectNames,
        string server, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(consumerProject.ProjectFolderName))
        {
            StShared.WriteWarningLine(
                $"ProjectFolderName does not specified for project {consumerProjectName}, skipped", true, _logger);
            return true;
        }

        //SpaProjectName-ის GitProjects ჩანაწერის გარეშე მხოლოდ ის შემთხვევა ჩერდება, როცა მთავარ ან პაკეტის
        //რეპოზიტორიას SPA-ს ფოლდერი სჭირდება
        (string? spaProjectFolderRelativePath, bool spaProjectIsMissing) =
            SpaProjectFolderHelper.GetSpaProjectFolderRelativePath(_logger, consumerProjectName, consumerProject,
                gitProjects, parameters.Gits, [consumerProjectName, .. packageProjectNames], true);
        if (spaProjectIsMissing)
        {
            return false;
        }

        var gitRepos = GitRepos.Create(_logger, parameters.Gits, spaProjectFolderRelativePath, true, false);

        //მომხმარებელი პროექტის მთავარი რეპოზიტორია არის ის, რომლის სახელიც ემთხვევა პროექტის სახელს
        if (!consumerProject.GitProjectNames.Contains(consumerProjectName) ||
            !gitRepos.Gits.TryGetValue(consumerProjectName, out GitData? mainGitData))
        {
            StShared.WriteWarningLine($"Main git repository does not found for project {consumerProjectName}, skipped",
                true, _logger);
            return true;
        }

        string mainRepoPath =
            Path.GetFullPath(Path.Combine(consumerProject.ProjectFolderName, mainGitData.GitProjectFolderName));

        if (!Directory.Exists(mainRepoPath))
        {
            StShared.WriteWarningLine(
                $"Main repository folder {mainRepoPath} does not exists for project {consumerProjectName}, skipped",
                true, _logger);
            return true;
        }

        //პაკეტების git-ების ცვლილებები ამავე გაშვებაში ხშირად იგზავნება remote-ზე, მომხმარებელი პროექტის გვერდით მათი
        //კლონები კი ძველი რჩება. ამ კლონების პროექტებზე დარჩენილი რეფერენსები მაშინ ძველ ვერსიებს ითხოვს და ახალ
        //პაკეტთან კონფლიქტს იწვევს (NU1107, NU1605). ამიტომ ვერსიების აწევამდე და რეფერენსების ჩანაცვლებამდე ყველა
        //პაკეტის კლონი ახლდება: ერთის განახლების შემდეგ restore-მა მეორე, ჯერ ძველი კლონი რომ ნახოს, ისიც ასეთ
        //კონფლიქტს იწვევს
        List<(string PackageProjectName, string PackageRepoPath)> packageRepos = [];
        foreach (string packageProjectName in packageProjectNames)
        {
            if (!gitRepos.Gits.TryGetValue(packageProjectName, out GitData? packageGitData))
            {
                StShared.WriteWarningLine(
                    $"Git repository {packageProjectName} does not found in gits list for project {consumerProjectName}, skipped",
                    true, _logger);
                continue;
            }

            string packageRepoPath = Path.GetFullPath(Path.Combine(consumerProject.ProjectFolderName,
                packageGitData.GitProjectFolderName));
            if (!PullPackageRepoCopy(consumerProjectName, packageRepoPath))
            {
                return false;
            }

            packageRepos.Add((packageProjectName, packageRepoPath));
        }

        bool hadErrors = false;
        foreach ((string packageProjectName, string packageRepoPath) in packageRepos)
        {
            hadErrors |= !await DistributePackageToMainRepo(mainRepoPath, packageProjectName, packageRepoPath, server,
                cancellationToken);
        }

        return !hadErrors;
    }

    //ერთი პაკეტის გავრცელება მომხმარებელი პროექტის მთავარ რეპოზიტორიაში: ვერსიების აწევა და რეფერენსების ჩანაცვლება
    private async Task<bool> DistributePackageToMainRepo(string mainRepoPath, string packageProjectName,
        string packageRepoPath, string server, CancellationToken cancellationToken)
    {
        string packageRepoPrefix = packageRepoPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        //მანამდე უკვე ჩასმული ამავე პაკეტის პროექტის პაკეტების ვერსიები უახლესზე აიწევა,
        //რომ ძველ ვერსიებზე მიბმის გამო ვერსიების კონფლიქტები (NU1107) არ წარმოიშვას
        bool hadErrors = !await UpdateExistingPackageVersions(mainRepoPath, packageProjectName, packageRepoPath, server,
            cancellationToken);

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
            List<string> includes =
            [
                .. projectXml.Descendants("ProjectReference").Select(x => (string?)x.Attribute("Include"))
                    .OfType<string>()
            ];

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

                if (!_referenceReplacer.Replace(csprojFile, refFullPath, packageId, latestVersion))
                {
                    hadErrors = true;
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

    //მომხმარებელი პროექტის გვერდით მდებარე პაკეტის რეპოზიტორიის კლონის განახლება git pull --ff-only-ით. ლოკალური
    //ცვლილებების მქონე კლონს pull არ ეხება (ცვლილებები მომხმარებლისაა), pull-ის ჩავარდნისას კი პროექტი გამოიტოვება
    private bool PullPackageRepoCopy(string consumerProjectName, string packageRepoPath)
    {
        //კლონის გარეშე გასაახლებელი არაფერია
        if (!Directory.Exists(packageRepoPath))
        {
            return true;
        }

        var gitProcessor = new GitProcessor(true, _logger, packageRepoPath);
        Result<bool> needCommitResult = gitProcessor.NeedCommit();
        if (needCommitResult.IsFailure)
        {
            StShared.WriteErrorLine(
                $"Cannot check changes of package repository {packageRepoPath} for project {consumerProjectName}, skipped: {needCommitResult.Error.Description}",
                true, _logger);
            return false;
        }

        if (needCommitResult.Value)
        {
            StShared.WriteWarningLine(
                $"Package repository {packageRepoPath} has local changes and was not pulled for project {consumerProjectName}",
                true, _logger);
            return true;
        }

        if (gitProcessor.PullFastForwardOnly().IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine(
            $"Cannot pull package repository {packageRepoPath} with --ff-only for project {consumerProjectName}, skipped",
            true, _logger);
        return false;
    }

    //მომხმარებელი პროექტის მთავარ რეპოზიტორიაში უკვე არსებული ამ პაკეტის პროექტის პაკეტების
    //ვერსიების აწევა უახლესზე Directory.Packages.props ფაილებში
    private async Task<bool> UpdateExistingPackageVersions(string mainRepoPath, string packageProjectName,
        string packageRepoPath, string server, CancellationToken cancellationToken)
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
                                               packageId.Equals(packageProjectName,
                                                   StringComparison.OrdinalIgnoreCase) ||
                                               packageId.StartsWith(packageProjectName + ".",
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

    private async Task<string?> GetPackageBaseAddress(string server, CancellationToken cancellationToken)
    {
        _packageBaseAddress ??= await FindPackageBaseAddress(_httpClientFactory, server, _logger, cancellationToken);
        return _packageBaseAddress;
    }

    //flat container-ის საბაზისო მისამართის დადგენა სერვერის service index-იდან.
    //სხვადასხვა სერვერს ეს მისამართი სხვადასხვა აქვს (მაგალითად BaGetter-ს /v3/package/, nuget.org-ს /v3-flatcontainer/),
    //ამიტომ მისი აღმოჩენა ხდება PackageBaseAddress რესურსით
    internal static async Task<string?> FindPackageBaseAddress(IHttpClientFactory httpClientFactory, string server,
        ILogger logger, CancellationToken cancellationToken)
    {
        var serviceIndexUri = new Uri(NugetServerUrls.GetServiceIndexUrl(server));

        // ReSharper disable once using
        using HttpClient client = httpClientFactory.CreateClient();
        // ReSharper disable once using
        using HttpResponseMessage response = await client.GetAsync(serviceIndexUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            StShared.WriteErrorLine(
                $"Cannot get nuget service index {serviceIndexUri}. Status code is {response.StatusCode}", true,
                logger);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        string? packageBaseAddress = JObject.Parse(content)["resources"]
            ?.FirstOrDefault(x => (string?)x["@type"] == "PackageBaseAddress/3.0.0")?["@id"]?.ToString();

        if (string.IsNullOrWhiteSpace(packageBaseAddress))
        {
            StShared.WriteErrorLine(
                $"PackageBaseAddress resource does not found in nuget service index {serviceIndexUri}", true, logger);
            return null;
        }

        return packageBaseAddress.TrimEnd('/');
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

        //NuGet-ის flat container-ის URL-ში პაკეტის id პატარა ასოებით ეწერება
#pragma warning disable CA1308
        var uri = new Uri($"{packageBaseAddress}/{packageId.ToLowerInvariant()}/index.json");
#pragma warning restore CA1308

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
    internal static string GetLatestVersion(List<string> versions)
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
