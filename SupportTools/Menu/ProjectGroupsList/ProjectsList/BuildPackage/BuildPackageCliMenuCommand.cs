using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppCliTools.CliMenu;
using LibDotnetWork;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.ApiClientsManagement;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;

public sealed class BuildPackageCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Build Package";

    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BuildPackageCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName) : base(
        MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, projectName)
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

        var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);

        string? mainProjectFileName = project.MainProjectFileName(gitProjects);
        if (mainProjectFileName is null)
        {
            StShared.WriteErrorLine($"Main project does not specified for {_projectName}", true);
            return ValueTask.FromResult(false);
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
            return ValueTask.FromResult(false);
        }

        string packageVersion = CreatePackageVersion(mainProjectFileName);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("dotnet packing with packageVersion {PackageVersion} ...", packageVersion);
        }

        var dotnetProcessor = new DotnetProcessor(_logger, true);

        //პაკეტის დაბილდვა
        if (dotnetProcessor.Pack(mainProjectFileName, outputFolderPath, packageVersion).IsSome)
        {
            StShared.WriteErrorLine($"Cannot pack project {_projectName}", true, _logger);
            return ValueTask.FromResult(false);
        }

        string nugetSourceUrl = CreateNugetSourceUrl(apiClientSettings.Server);

        //პაკეტის ატვირთვა package manager-ზე
        if (dotnetProcessor.NugetPush(Path.Combine(outputFolderPath, "*.nupkg"), nugetSourceUrl,
                apiClientSettings.ApiKey).IsSome)
        {
            StShared.WriteErrorLine($"Cannot push package for project {_projectName}", true, _logger);
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    //პაკეტის ვერსიის შექმნა csproj ფაილში მითითებული ვერსიისა და მიმდინარე თარიღის მიხედვით
    private static string CreatePackageVersion(string mainProjectFileName)
    {
        XElement projectXml = XElement.Load(mainProjectFileName);

        XElement? xmlVersionId = projectXml.Descendants("PropertyGroup").Descendants("Version").SingleOrDefault();

        string versionId = xmlVersionId?.Value ?? "1.0.0";

        string[] verNumbers = versionId.Split('.');
        var packageVersionNumbers = new List<string>();
        packageVersionNumbers.AddRange(verNumbers.Take(2));
        if (packageVersionNumbers.Count == 0)
        {
            packageVersionNumbers.Add("1");
        }

        if (packageVersionNumbers.Count == 1)
        {
            packageVersionNumbers.Add("0");
        }

        DateTime todayDate = DateTime.Today;
        DateTime now = DateTime.Now;

        packageVersionNumbers.Add(
            (todayDate - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays.ToString(CultureInfo
                .InvariantCulture));
        packageVersionNumbers.Add(((int)(now - todayDate).TotalSeconds / 2).ToString(CultureInfo.InvariantCulture));
        return string.Join('.', packageVersionNumbers);
    }

    //package manager-ის მისამართი შეიძლება ბოლოვდებოდეს /api/v1-ის მსგავსი სუფიქსით,
    //push-ისთვის კი საჭიროა nuget-ის service index-ის მისამართი
    private static string CreateNugetSourceUrl(string server)
    {
        string baseUrl = server.TrimEnd('/');
        int lastSlashIndex = baseUrl.LastIndexOf('/');
        if (lastSlashIndex > 0)
        {
            string lastSegment = baseUrl[(lastSlashIndex + 1)..];
            string beforeLastSegment = baseUrl[..lastSlashIndex];
            if (lastSegment.Length > 1 && (lastSegment[0] == 'v' || lastSegment[0] == 'V') &&
                lastSegment[1..].All(char.IsAsciiDigit) &&
                beforeLastSegment.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = beforeLastSegment[..^4];
            }
        }

        return baseUrl + "/v3/index.json";
    }
}
