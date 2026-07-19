using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.RemoveUnusedPackageVersions;

public sealed class RemoveUnusedPackageVersionsCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Remove Unused Package Versions";

    private const string PropsFileName = "Directory.Packages.props";

    private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
    private static readonly string[] ConsumerSearchPatterns = ["*.csproj", "*.props", "*.targets"];

    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RemoveUnusedPackageVersionsCliMenuCommand(ILogger logger, ParametersManager parametersManager) : base(
        MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            "This process will analyze Directory.Packages.props files of all registered projects and remove PackageVersion entries that are not used by any project");
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ერთი და იგივე props ფაილი შესაძლოა რამდენიმე პროექტმა გამოიყენოს, ამიტომ დამუშავებულების სია ინახება
        var processedPropsFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool hadErrors = false;
        int changedFilesCount = 0;
        int removedEntriesCount = 0;

        foreach ((string projectName, ProjectModel project) in parameters.Projects.OrderBy(x => x.Key,
                     StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (string propsFile in CollectPropsFiles(projectName, project))
            {
                if (!processedPropsFiles.Add(propsFile))
                {
                    continue;
                }

                int? removedCount = ProcessPropsFile(propsFile);

                if (removedCount is null)
                {
                    hadErrors = true;
                    continue;
                }

                if (removedCount.Value == 0)
                {
                    continue;
                }

                changedFilesCount++;
                removedEntriesCount += removedCount.Value;
            }
        }

        Console.WriteLine(
            $"Remove unused package versions finished. Props files processed: {processedPropsFiles.Count}, files changed: {changedFilesCount}, entries removed: {removedEntriesCount}");

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Remove unused package versions finished. Props files processed: {ProcessedFilesCount}, files changed: {ChangedFilesCount}, entries removed: {RemovedEntriesCount}",
                processedPropsFiles.Count, changedFilesCount, removedEntriesCount);
        }

        return ValueTask.FromResult(!hadErrors);
    }

    //პროექტისთვის Directory.Packages.props ფაილების მოძებნა სოლუშენის ფოლდერიდან
    private List<string> CollectPropsFiles(string projectName, ProjectModel project)
    {
        if (string.IsNullOrWhiteSpace(project.SolutionFileName))
        {
            StShared.WriteWarningLine($"SolutionFileName does not specified for project {projectName}, skipped", true,
                _logger);
            return [];
        }

        string? solutionFolder = Path.GetDirectoryName(project.SolutionFileName);

        if (string.IsNullOrWhiteSpace(solutionFolder) || !Directory.Exists(solutionFolder))
        {
            StShared.WriteWarningLine($"Solution folder does not exists for project {projectName}, skipped", true,
                _logger);
            return [];
        }

        List<string> propsFiles = Directory
            .EnumerateFiles(Path.GetFullPath(solutionFolder), PropsFileName, SearchOption.AllDirectories)
            .Where(x => !IsInBinOrObjFolder(x)).Select(x => Path.GetFullPath(x)).ToList();

        if (propsFiles.Count == 0)
        {
            StShared.WriteWarningLine($"{PropsFileName} does not found for project {projectName}, skipped", true,
                _logger);
        }

        return propsFiles;
    }

    //ერთი props ფაილის ანალიზი და გამოუყენებელი PackageVersion ჩანაწერების წაშლა.
    //წარმატებისას ბრუნდება წაშლილი ჩანაწერების რაოდენობა, შეცდომისას — null და ფაილი უცვლელი რჩება
    private int? ProcessPropsFile(string propsFile)
    {
        StShared.ConsoleWriteInformationLine(_logger, true, $"Processing {propsFile}");

        string? propsFolder = Path.GetDirectoryName(propsFile);

        if (string.IsNullOrWhiteSpace(propsFolder))
        {
            StShared.WriteErrorLine($"Cannot detect folder for {propsFile}", true, _logger);
            return null;
        }

        HashSet<string>? usedPackageIds = CollectUsedPackageIds(propsFolder);

        if (usedPackageIds is null)
        {
            return null;
        }

        try
        {
            XDocument propsXml = XDocument.Load(propsFile, LoadOptions.PreserveWhitespace);

            List<XElement> unusedElements = [];

            foreach (XElement packageVersionElement in propsXml.Descendants()
                         .Where(x => x.Name.LocalName == "PackageVersion"))
            {
                string? packageId = (string?)packageVersionElement.Attribute("Include");

                //Include ატრიბუტის გარეშე (Update/Remove სტილის) ან MSBuild თვისებიანი ჩანაწერები არ იშლება
                if (string.IsNullOrWhiteSpace(packageId) || packageId.Contains("$(", StringComparison.Ordinal))
                {
                    continue;
                }

                if (usedPackageIds.Contains(packageId))
                {
                    continue;
                }

                //პირობიანი ჩანაწერების წაშლა სახიფათოა, რადგან პირობები აქ არ ფასდება
                if (packageVersionElement.AncestorsAndSelf().Any(x => x.Attribute("Condition") is not null))
                {
                    StShared.WriteWarningLine(
                        $"Package {packageId} looks unused in {propsFile}, but the PackageVersion element is conditional, left in place",
                        true, _logger);
                    continue;
                }

                unusedElements.Add(packageVersionElement);
            }

            if (unusedElements.Count == 0)
            {
                return 0;
            }

            var touchedItemGroups = new HashSet<XElement>();

            foreach (XElement unusedElement in unusedElements)
            {
                string? packageId = (string?)unusedElement.Attribute("Include");

                if (unusedElement.Parent is { } parentElement && parentElement.Name.LocalName == "ItemGroup")
                {
                    touchedItemGroups.Add(parentElement);
                }

                RemoveNodeWithLeadingWhitespace(unusedElement);
                Console.WriteLine($"  Removed unused package version {packageId}");
            }

            //მთლიანად დაცარიელებული ItemGroup ელემენტებიც იშლება (კომენტარიანები რჩება)
            foreach (XElement itemGroup in touchedItemGroups.Where(x =>
                         !x.Elements().Any() && !x.Nodes().OfType<XComment>().Any()))
            {
                RemoveNodeWithLeadingWhitespace(itemGroup);
            }

            propsXml.Save(propsFile, SaveOptions.DisableFormatting);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Removed {RemovedCount} unused package versions from {PropsFile}",
                    unusedElements.Count, propsFile);
            }

            return unusedElements.Count;
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            StShared.WriteException(e, true, _logger);
            return null;
        }
    }

    //props ფაილის ფოლდერში არსებული ყველა csproj, props და targets ფაილიდან გამოყენებული პაკეტების სიის შეგროვება.
    //null ბრუნდება, თუ ანალიზი სანდო არ არის და props ფაილს ხელი არ უნდა ვახლოთ
    private HashSet<string>? CollectUsedPackageIds(string propsFolder)
    {
        var usedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int projectFilesCount = 0;

        try
        {
            foreach (string consumerFile in ConsumerSearchPatterns
                         .SelectMany(x => Directory.EnumerateFiles(propsFolder, x, SearchOption.AllDirectories))
                         .Where(x => !IsInBinOrObjFolder(x)))
            {
                if (consumerFile.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    projectFilesCount++;
                }

                XDocument consumerXml = XDocument.Load(consumerFile);

                foreach (XElement referenceElement in consumerXml.Descendants()
                             .Where(x => x.Name.LocalName is "PackageReference" or "GlobalPackageReference"))
                {
                    if (!TryAddPackageId(usedPackageIds, (string?)referenceElement.Attribute("Include"),
                            consumerFile) ||
                        !TryAddPackageId(usedPackageIds, (string?)referenceElement.Attribute("Update"), consumerFile))
                    {
                        return null;
                    }
                }
            }
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            StShared.WriteException(e, true, _logger);
            return null;
        }

        if (projectFilesCount != 0)
        {
            return usedPackageIds;
        }

        StShared.WriteWarningLine($"No csproj files found under {propsFolder}, file left unchanged", true, _logger);
        return null;
    }

    //თუ პაკეტის სახელი MSBuild თვისებას შეიცავს, ანალიზი სანდო აღარ არის და false ბრუნდება
    private bool TryAddPackageId(HashSet<string> usedPackageIds, string? packageId, string consumerFile)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            return true;
        }

        if (packageId.Contains("$(", StringComparison.Ordinal))
        {
            StShared.WriteWarningLine(
                $"PackageReference with MSBuild property {packageId} found in {consumerFile}, analysis is not reliable, file left unchanged",
                true, _logger);
            return false;
        }

        usedPackageIds.Add(packageId);
        return true;
    }

    //ელემენტის წაშლისას მის წინ მდგომი თეთრი სივრცის ტექსტური კვანძიც იშლება, რომ ცარიელი ხაზები არ დარჩეს
    private static void RemoveNodeWithLeadingWhitespace(XElement element)
    {
        if (element.PreviousNode is XText textNode && string.IsNullOrWhiteSpace(textNode.Value))
        {
            textNode.Remove();
        }

        element.Remove();
    }

    private static bool IsInBinOrObjFolder(string fileFullPath)
    {
        return fileFullPath.Split(DirectorySeparators).Any(pathPart =>
            pathPart.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            pathPart.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }
}
