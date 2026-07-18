using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PackageBuilder(ILogger logger, ParametersManager parametersManager, string projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
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

        //იპაკება მთელი solution-ი, რომ ყველა packable პროექტის პაკეტი შეიქმნას და აიტვირთოს
        string? solutionFileName = project.SolutionFileName;
        if (string.IsNullOrWhiteSpace(solutionFileName))
        {
            StShared.WriteErrorLine($"Solution file name does not specified for {_projectName}", true);
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

        var dotnetProcessor = new DotnetProcessor(_logger, true);

        //პაკეტების დაბილდვა
        if (dotnetProcessor.Pack(solutionFileName, outputFolderPath, packageVersion).IsSome)
        {
            StShared.WriteErrorLine($"Cannot pack solution for project {_projectName}", true, _logger);
            return false;
        }

        string nugetSourceUrl = NugetServerUrls.GetServiceIndexUrl(apiClientSettings.Server);

        //პაკეტის ატვირთვა package manager-ზე
        if (!dotnetProcessor.NugetPush(Path.Combine(outputFolderPath, "*.nupkg"), nugetSourceUrl,
                apiClientSettings.ApiKey).IsSome)
        {
            return true;
        }

        StShared.WriteErrorLine($"Cannot push package for project {_projectName}", true, _logger);
        return false;
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
