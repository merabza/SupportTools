using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.ApiClientsManagement;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;

//პაკეტის დაბილდვისა და package manager-ზე ატვირთვის ლოგიკა. გამოიყენება როგორც ცალკე მენიუს ბრძანებიდან,
//ისე ყველა პაკეტის გავრცელების პროცესიდან
public sealed class PackageBuilder
{
    private readonly IPackageBuildCommands _commands;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    public PackageBuilder(ILogger logger, IParametersManager parametersManager, string projectName) : this(logger,
        parametersManager, projectName, new DotnetPackageBuildCommands(new DotnetProcessor(logger, true)))
    {
    }

    //dotnet-ის ბრძანებები პარამეტრადაა გამოტანილი, რომ ტესტებმა dotnet არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal PackageBuilder(ILogger logger, IParametersManager parametersManager, string projectName,
        IPackageBuildCommands commands)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _commands = commands;
    }

    public bool BuildAndUploadPackage()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return false;
        }

        //პაკეტის დაბილდვა მუშაობს მხოლოდ პაკეტის ტიპის პროექტებისთვის
        if (project.ProjectType != EProjectType.IsPackage)
        {
            StShared.WriteErrorLine($"Project {_projectName} is not package type project", true);
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

        //იპაკება მთელი solution-ი, რომ მისი ყველა packable პროექტის პაკეტი ერთი build-ით შეიქმნას
        string? solutionFileName = project.SolutionFileName;
        if (string.IsNullOrWhiteSpace(solutionFileName))
        {
            StShared.WriteErrorLine($"Solution file name does not specified for {_projectName}", true);
            return false;
        }

        string? solutionFolder = Path.GetDirectoryName(solutionFileName);
        if (string.IsNullOrWhiteSpace(solutionFolder))
        {
            StShared.WriteErrorLine($"Cannot get solution folder for {solutionFileName}", true);
            return false;
        }

        //პაკეტების output ფოლდერი ყოველ ჯერზე თავიდან იქმნება,
        //რომ ატვირთვისას მხოლოდ ახლად შექმნილი პაკეტები მოხვდეს
        string outputFolderPath = Path.Combine(Path.GetTempPath(), "SupportTools", "BuildPackage", _projectName);
        if (Directory.Exists(outputFolderPath))
        {
            Directory.Delete(outputFolderPath, true);
        }

        if (!StShared.CreateFolder(outputFolderPath, true))
        {
            return false;
        }

        string packageVersion = CreatePackageVersion(project.MajorVersion, project.MinorVersion);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("dotnet packing with packageVersion {PackageVersion} ...", packageVersion);
        }

        //პაკეტების დაბილდვა
        if (_commands.Pack(solutionFileName, outputFolderPath, packageVersion).IsFailure)
        {
            StShared.WriteErrorLine($"Cannot pack solution for project {_projectName}", true, _logger);
            return false;
        }

        //solution-ში სხვა რეპოზიტორიების პროექტებიც შედის (../SystemTools/...), რომლებიც ამავე ვერსიით იპაკება. მათი
        //ატვირთული ასლი სერვერზე მათ უახლეს ვერსიად ჩანს და გავრცელება სწორედ მას აიღებს, ზოგი ასლი კი ისეთ ვერსიაზეა
        //დამოკიდებული, რომელიც არასდროს აიტვირთება (NU1102). ამიტომ იტვირთება მხოლოდ ამ რეპოზიტორიის პაკეტები
        Dictionary<string, string> ownPackageFiles =
            RemoveOtherRepositoriesPackages(solutionFolder, outputFolderPath, packageVersion);
        if (ownPackageFiles.Count == 0)
        {
            StShared.WriteErrorLine($"No packages of project {_projectName} were created", true, _logger);
            return false;
        }

        if (!CheckOwnPackageDependencies(ownPackageFiles, packageVersion))
        {
            return false;
        }

        string nugetSourceUrl = NugetServerUrls.GetServiceIndexUrl(apiClientSettings.Server);

        //პაკეტის ატვირთვა package manager-ზე
        if (!_commands.NugetPush(Path.Combine(outputFolderPath, "*.nupkg"), nugetSourceUrl, apiClientSettings.ApiKey)
                .IsFailure)
        {
            return true;
        }

        StShared.WriteErrorLine($"Cannot push package for project {_projectName}", true, _logger);
        return false;
    }

    //output ფოლდერიდან იშლება სხვა რეპოზიტორიების პაკეტები. ამ რეპოზიტორიისაა solution-ის ფოლდერში მდებარე პროექტების
    //პაკეტები: პაკეტის სახელი ემთხვევა csproj ფაილის სახელს, პაკეტის ფაილისა კი {პაკეტის სახელი}.{ვერსია}.nupkg.
    //აბრუნებს ამ რეპოზიტორიის პაკეტების ფაილებს პაკეტების სახელების მიხედვით
    private Dictionary<string, string> RemoveOtherRepositoriesPackages(string solutionFolder, string outputFolderPath,
        string packageVersion)
    {
        HashSet<string> ownProjectNames = Directory
            .EnumerateFiles(solutionFolder, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        string packageFileNameSuffix = $".{packageVersion}.nupkg";
        var ownPackageFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<string> otherPackageIds = [];
        foreach (string packageFile in Directory.GetFiles(outputFolderPath, "*.nupkg"))
        {
            string fileName = Path.GetFileName(packageFile);
            string packageId = fileName.EndsWith(packageFileNameSuffix, StringComparison.OrdinalIgnoreCase)
                ? fileName[..^packageFileNameSuffix.Length]
                : fileName;
            if (ownProjectNames.Contains(packageId))
            {
                ownPackageFiles.Add(packageId, packageFile);
                continue;
            }

            File.Delete(packageFile);
            otherPackageIds.Add(packageId);
        }

        if (otherPackageIds.Count > 0)
        {
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Packages of other repositories are not uploaded: {string.Join(", ", otherPackageIds.Order(StringComparer.Ordinal))}");
        }

        return ownPackageFiles;
    }

    //ProjectReference პაკეტის დამოკიდებულებად ამავე ვერსიით იწერება. თუ ის ამ რეპოზიტორიის შექმნილი პაკეტი არ არის,
    //ატვირთული პაკეტი სერვერზე არარსებულ ვერსიას მოითხოვდა, ამიტომ ასეთ დროს არაფერი იტვირთება
    private bool CheckOwnPackageDependencies(Dictionary<string, string> ownPackageFiles, string packageVersion)
    {
        List<string> missingDependencies =
        [
            .. ownPackageFiles.OrderBy(x => x.Key, StringComparer.Ordinal).SelectMany(x =>
                ReadPackageDependencies(x.Key, x.Value)
                    .Where(dependency => dependency.Id is not null &&
                                         string.Equals(dependency.Version, packageVersion,
                                             StringComparison.OrdinalIgnoreCase) &&
                                         !ownPackageFiles.ContainsKey(dependency.Id))
                    .Select(dependency => $"{x.Key} -> {dependency.Id}"))
        ];

        if (missingDependencies.Count == 0)
        {
            return true;
        }

        StShared.WriteErrorLine(
            $"Packages of project {_projectName} depend on packages of version {packageVersion} that are not uploaded: {string.Join(", ", missingDependencies)}. A package can use projects of another repository only through their packages",
            true, _logger);
        return false;
    }

    //პაკეტის nuspec ფაილის დამოკიდებულებები. nuspec ფაილი არქივის ძირშია და პაკეტის სახელი ჰქვია
    private static List<(string? Id, string? Version)> ReadPackageDependencies(string packageId, string packageFile)
    {
        // ReSharper disable once using
        using ZipArchive packageArchive = ZipFile.OpenRead(packageFile);
        ZipArchiveEntry? nuspecEntry = packageArchive.GetEntry($"{packageId}.nuspec");
        if (nuspecEntry is null)
        {
            return [];
        }

        // ReSharper disable once using
        using Stream nuspecStream = nuspecEntry.Open();
        return
        [
            .. XDocument.Load(nuspecStream).Descendants().Where(x => x.Name.LocalName == "dependency")
                .Select(x => ((string?)x.Attribute("id"), (string?)x.Attribute("version")))
        ];
    }

    //პაკეტის ვერსიის შექმნა პროექტში მითითებული MajorVersion, MinorVersion ვერსიებისა და მიმდინარე თარიღის მიხედვით
    private static string CreatePackageVersion(int majorVersion, int minorVersion)
    {
        DateTime todayDate = DateTime.Today;
        DateTime now = DateTime.Now;

        var packageVersionNumbers = new List<string>
        {
            majorVersion.ToString(CultureInfo.InvariantCulture),
            minorVersion.ToString(CultureInfo.InvariantCulture),
            (todayDate - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays.ToString(CultureInfo
                .InvariantCulture),
            ((int)(now - todayDate).TotalSeconds / 2).ToString(CultureInfo.InvariantCulture)
        };
        return string.Join('.', packageVersionNumbers);
    }
}
