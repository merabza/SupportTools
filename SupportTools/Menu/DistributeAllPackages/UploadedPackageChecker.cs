using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using LibGitWork;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.DistributeAllPackages;

//პაკეტის პროექტის რეპოზიტორიის შედარება მისი პაკეტების ბოლო ატვირთვასთან. dotnet pack-ის SourceLink-ი პაკეტის
//nuspec-ში იმ commit-ს წერს, რომლიდანაც პაკეტი დაიბილდა (<repository commit="..." />), ამიტომ ატვირთვები ლოკალურად არ
//აღირიცხება: სერვერზე ამ რეპოზიტორიის პაკეტების უახლესი ვერსიის commit-ი რეპოზიტორიის HEAD-ს ედრება. ასე სხვა
//კომპიუტერიდან ატვირთული პაკეტიც ითვლება
internal sealed class UploadedPackageChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadedPackageChecker(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    //ბოლოს ატვირთული პაკეტების ვერსია, თუ რეპოზიტორია მას მერე არ შეცვლილა; null - პაკეტები უნდა დაიბილდოს და
    //აიტვირთოს. ყოველი გაურკვეველი შემთხვევა null-ია: ზედმეტი დაბილდვა მხოლოდ დროს კარგავს, შეცვლილი პაკეტის
    //გამოტოვება კი მომხმარებელ პროექტებს ძველ პაკეტს დაუტოვებდა
    public async Task<string?> GetUnchangedUploadedVersion(string projectName, CancellationToken cancellationToken)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        string? solutionFileName = parameters.GetProject(projectName)?.SolutionFileName;
        string? solutionFolder =
            string.IsNullOrWhiteSpace(solutionFileName) ? null : Path.GetDirectoryName(solutionFileName);
        string? apiClientName = parameters.LocalPackageManagerWebApiClientName;
        string? server = string.IsNullOrWhiteSpace(apiClientName)
            ? null
            : parameters.ApiClients.GetValueOrDefault(apiClientName)?.Server;

        //პარამეტრების ნაკლს პაკეტის შემოწმებისა და დაბილდვის ნაბიჯები თვითონ აჩვენებს
        if (string.IsNullOrWhiteSpace(solutionFolder) || string.IsNullOrWhiteSpace(server))
        {
            return null;
        }

        string? headCommitId = GetCleanHeadCommitId(projectName, solutionFolder);
        if (headCommitId is null)
        {
            return null;
        }

        try
        {
            return await GetUploadedVersionOfCommit(projectName, solutionFolder, server, headCommitId,
                cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or XmlException)
        {
            WriteCannotCheckWarning(projectName, e.Message);
            return null;
        }
    }

    //რეპოზიტორიის HEAD, თუ მასში დაუკომიტებელი ცვლილებები არ არის: ასეთი ცვლილებები ატვირთულ პაკეტებში არ შედის
    private string? GetCleanHeadCommitId(string projectName, string solutionFolder)
    {
        var gitProcessor = new GitProcessor(false, _logger, solutionFolder);
        Result<bool> needCommitResult = gitProcessor.NeedCommit();
        if (needCommitResult.IsFailure)
        {
            WriteCannotCheckWarning(projectName, needCommitResult.Error.Description);
            return null;
        }

        if (needCommitResult.Value)
        {
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Project {projectName} has uncommitted changes, its packages will be built and uploaded");
            return null;
        }

        Result<string> headCommitIdResult = gitProcessor.GetHeadCommitId();
        if (headCommitIdResult.IsSuccess)
        {
            return headCommitIdResult.Value;
        }

        WriteCannotCheckWarning(projectName, headCommitIdResult.Error.Description);
        return null;
    }

    //ბოლოს ატვირთული პაკეტების ვერსია, თუ ისინი ამ commit-იდან დაიბილდა
    private async Task<string?> GetUploadedVersionOfCommit(string projectName, string solutionFolder, string server,
        string headCommitId, CancellationToken cancellationToken)
    {
        string? packageBaseAddress =
            await PackageDistributor.FindPackageBaseAddress(_httpClientFactory, server, _logger, cancellationToken);
        if (packageBaseAddress is null)
        {
            return null;
        }

        // ReSharper disable once using
        using HttpClient client = _httpClientFactory.CreateClient();

        //ამ რეპოზიტორიის პროექტების პაკეტების უახლესი ვერსიები. პროექტი, რომლისგანაც პაკეტი არ იბილდება (მაგალითად
        //ტესტების), სერვერზე არ არის
        var latestVersions = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string packageId in PackageBuilder.GetOwnProjectNames(solutionFolder).Order(StringComparer.Ordinal))
        {
            List<string>? versions = await GetPackageVersions(client, packageBaseAddress, packageId, cancellationToken);
            if (versions is { Count: > 0 })
            {
                latestVersions.Add(packageId, PackageDistributor.GetLatestVersion(versions));
            }
        }

        if (latestVersions.Count == 0)
        {
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Packages of project {projectName} are not on the package server yet, they will be built and uploaded");
            return null;
        }

        //ერთი ატვირთვის ყველა პაკეტს ერთი ვერსია აქვს. სხვადასხვა უახლესი ვერსია ნიშნავს, რომ ბოლო ატვირთვა ნაწილობრივ
        //ჩავარდა ან რომელიმე პროექტი მასში აღარ შევიდა
        if (latestVersions.Values.Distinct(StringComparer.OrdinalIgnoreCase).Skip(1).Any())
        {
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Packages of project {projectName} have different latest versions on the package server ({string.Join(", ", latestVersions.Select(x => $"{x.Key} {x.Value}"))}), they will be built and uploaded");
            return null;
        }

        (string packageIdToCheck, string version) = latestVersions.First();
        string? uploadedCommitId =
            await GetPackageCommitId(client, packageBaseAddress, packageIdToCheck, version, cancellationToken);
        if (uploadedCommitId is null)
        {
            StShared.ConsoleWriteInformationLine(_logger, true,
                $"Package {packageIdToCheck} {version} does not name its git commit, packages of project {projectName} will be built and uploaded");
            return null;
        }

        if (string.Equals(uploadedCommitId, headCommitId, StringComparison.OrdinalIgnoreCase))
        {
            return version;
        }

        StShared.ConsoleWriteInformationLine(_logger, true,
            $"Project {projectName} has changed since its packages {version} were uploaded (commit {ShortCommitId(uploadedCommitId)} -> {ShortCommitId(headCommitId)}), they will be built and uploaded");
        return null;
    }

    //პაკეტის ვერსიები flat container-იდან; null - ასეთი პაკეტი სერვერზე არ არის
    private static async Task<List<string>?> GetPackageVersions(HttpClient client, string packageBaseAddress,
        string packageId, CancellationToken cancellationToken)
    {
        //NuGet-ის flat container-ის URL-ში პაკეტის id პატარა ასოებით ეწერება
#pragma warning disable CA1308
        var uri = new Uri($"{packageBaseAddress}/{packageId.ToLowerInvariant()}/index.json");
#pragma warning restore CA1308

        // ReSharper disable once using
        using HttpResponseMessage response = await client.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JObject.Parse(content)["versions"]?.ToObject<List<string>>();
    }

    //commit-ი, რომლიდანაც პაკეტი დაიბილდა: nuspec-ის <repository commit="..." />
    private static async Task<string?> GetPackageCommitId(HttpClient client, string packageBaseAddress,
        string packageId, string version, CancellationToken cancellationToken)
    {
#pragma warning disable CA1308
        string lowerPackageId = packageId.ToLowerInvariant();
#pragma warning restore CA1308
        var uri = new Uri($"{packageBaseAddress}/{lowerPackageId}/{version}/{lowerPackageId}.nuspec");

        // ReSharper disable once using
        using HttpResponseMessage response = await client.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        //სერვერი nuspec-ს BOM-ით აბრუნებს: ნაკადიდან კითხვისას XmlReader-ი მას ამოიცნობს, სტრიქონიდან კი
        //XDocument.Parse ჩავარდებოდა
        // ReSharper disable once using
        await using Stream nuspecStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        XDocument nuspec = await XDocument.LoadAsync(nuspecStream, LoadOptions.None, cancellationToken);
        string? commitId = (string?)nuspec.Descendants().FirstOrDefault(x => x.Name.LocalName == "repository")
            ?.Attribute("commit");
        return string.IsNullOrWhiteSpace(commitId) ? null : commitId;
    }

    private void WriteCannotCheckWarning(string projectName, string reason)
    {
        StShared.WriteWarningLine(
            $"Cannot check the last upload of project {projectName}, its packages will be built and uploaded: {reason}",
            true, _logger);
    }

    private static string ShortCommitId(string commitId)
    {
        return commitId.Length > 7 ? commitId[..7] : commitId;
    }
}
