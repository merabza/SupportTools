using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LibDotnetWork;
using OneOf;
using ParametersManagement.LibParameters;
using SupportTools.Errors;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace SupportTools.Tools;

public static class DotnetToolsVersionsCheckerUpdater
{
    public static bool Check(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        StShared.ConsoleWriteInformationLine(null, true, "Checking versions for all tools...");
        OneOf<bool, Err[]> checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
        StShared.ConsoleWriteInformationLine(null, true, "Checking versions for all tools Finished.");

        return checkVersionsForAllToolsResult.Match(t0 => t0, t1 =>
        {
            Err.PrintErrorsOnConsole(t1);
            return false;
        });
    }

    public static bool CheckOne(IParametersManager parametersManager, string toolKey)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        if (!parameters.DotnetTools.TryGetValue(toolKey, out DotnetToolData? value))
        {
            StShared.WriteErrorLine($"Tool with key {toolKey} not found.", true);
            return false;
        }

        StShared.ConsoleWriteInformationLine(null, true, "Checking versions for tool {0}...", toolKey);
        OneOf<bool, Err[]> checkVersionsForOneToolResult = CheckVersionsForOneTool(value, null);
        if (checkVersionsForOneToolResult.IsT0)
        {
            return true;
        }

        Err.PrintErrorsOnConsole(checkVersionsForOneToolResult.AsT1);
        return false;
    }

    public static bool UpdateOne(IParametersManager parametersManager, string toolKey)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        if (!parameters.DotnetTools.TryGetValue(toolKey, out DotnetToolData? dotnetTool))
        {
            StShared.WriteErrorLine($"Tool with key {toolKey} not found.", true);
            return false;
        }

        OneOf<bool, Err[]> checkVersionsForOneToolResult = CheckVersionsForOneTool(dotnetTool, null);
        if (checkVersionsForOneToolResult.IsT1)
        {
            Err.PrintErrorsOnConsole(checkVersionsForOneToolResult.AsT1);
            return false;
        }

        if (!checkVersionsForOneToolResult.AsT0)
        {
            return true;
        }

        OneOf<bool, Err[]> updateOneToolToLatestVersionResult = UpdateOneToolToLatestVersion(dotnetTool);
        if (updateOneToolToLatestVersionResult.IsT1)
        {
            Err.PrintErrorsOnConsole(updateOneToolToLatestVersionResult.AsT1);
            return false;
        }

        checkVersionsForOneToolResult = CheckVersionsForOneTool(dotnetTool, null);
        if (!checkVersionsForOneToolResult.IsT1)
        {
            return true;
        }

        Err.PrintErrorsOnConsole(checkVersionsForOneToolResult.AsT1);
        return false;
    }

    public static bool UpdateAllToolsToLatestVersion(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        StShared.ConsoleWriteInformationLine(null, true, "Checking for tools Updates...");
        OneOf<bool, Err[]> checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
        if (checkVersionsForAllToolsResult.IsT1)
        {
            Err.PrintErrorsOnConsole(checkVersionsForAllToolsResult.AsT1);
            return false;
        }

        Dictionary<string, DotnetToolData> dotnetTools = parameters.DotnetTools;

        bool atLeastOneUpdatedOrInstalled = false;
        foreach (OneOf<bool, Err[]> updateOneToolToLatestVersionResult in dotnetTools.Select(kvp =>
                     UpdateOneToolToLatestVersion(kvp.Value)))
        {
            if (updateOneToolToLatestVersionResult.IsT1)
            {
                Err.PrintErrorsOnConsole(updateOneToolToLatestVersionResult.AsT1);
                return false;
            }

            atLeastOneUpdatedOrInstalled = updateOneToolToLatestVersionResult.AsT0 || atLeastOneUpdatedOrInstalled;
        }

        if (atLeastOneUpdatedOrInstalled)
        {
            StShared.ConsoleWriteInformationLine(null, true, "Updating tools List...");
            checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
            if (checkVersionsForAllToolsResult.IsT1)
            {
                Err.PrintErrorsOnConsole(checkVersionsForAllToolsResult.AsT1);
                return false;
            }

            StShared.ConsoleWriteInformationLine(null, true, "Updating process Finished.");
        }
        else
        {
            StShared.ConsoleWriteInformationLine(null, true, "All tools already are up to date.");
        }

        return true;
    }

    private static OneOf<bool, Err[]> UpdateOneToolToLatestVersion(DotnetToolData dotnetToolData)
    {
        if (string.IsNullOrWhiteSpace(dotnetToolData.PackageId) ||
            string.IsNullOrWhiteSpace(dotnetToolData.LatestVersion) || dotnetToolData.LatestVersion == "N/A" ||
            dotnetToolData.InstalledVersion == (string.IsNullOrWhiteSpace(dotnetToolData.MaxVersion)
                ? dotnetToolData.LatestVersion
                : dotnetToolData.MaxVersion))
        {
            return false;
        }

        bool toolInstalled = dotnetToolData.InstalledVersion != "N/A";
        string command = toolInstalled ? "update" : "install";
        StShared.ConsoleWriteInformationLine(null, true, "{0}ing {1}...", command, dotnetToolData.PackageId);

        var dotnetProcessor = new DotnetProcessor(null, false);
        Option<Err[]> result = toolInstalled
            ? dotnetProcessor.UpdateTool(dotnetToolData.PackageId, dotnetToolData.MaxVersion)
            : dotnetProcessor.InstallTool(dotnetToolData.PackageId, dotnetToolData.MaxVersion);
        return result.Match<OneOf<bool, Err[]>>(some => some, true);
    }

    private static OneOf<bool, Err[]> CheckVersionsForAllTools(Dictionary<string, DotnetToolData> necessaryDotnetTools)
    {
        StShared.ConsoleWriteInformationLine(null, true, "Create List of Installed tools...");
        OneOf<List<DotnetToolData>, Err[]> createListOfDotnetToolsInstalledResult = CreateListOfDotnetToolsInstalled();
        if (createListOfDotnetToolsInstalledResult.IsT1)
        {
            return Err.RecreateErrors(createListOfDotnetToolsInstalledResult.AsT1,
                DotnetToolsManagerErrors.CreateListOfDotnetToolsInstalledError);
        }

        List<DotnetToolData>? listOfToolsInstalled = createListOfDotnetToolsInstalledResult.AsT0;

        List<Err> errors = [];
        bool madeChanges = false;

        foreach (KeyValuePair<string, DotnetToolData> kvp in necessaryDotnetTools)
        {
            OneOf<bool, Err[]> checkVersionsForOneToolResult = CheckVersionsForOneTool(kvp.Value, listOfToolsInstalled);
            if (checkVersionsForOneToolResult.IsT1)
            {
                errors.AddRange(Err.RecreateErrors(checkVersionsForOneToolResult.AsT1,
                    DotnetToolsManagerErrors.CheckVersionsForOneToolError(kvp.Key)));
            }

            madeChanges = checkVersionsForOneToolResult.AsT0;
        }

        if (errors.Count > 0)
        {
            return errors.ToArray();
        }

        return madeChanges;
    }

    private static OneOf<bool, Err[]> CheckVersionsForOneTool(DotnetToolData dotnetToolData,
        List<DotnetToolData>? listOfToolsInstalled)
    {
        string? packageId = dotnetToolData.PackageId;
        if (string.IsNullOrEmpty(packageId))
        {
            return Err.CreateArr(DotnetToolsManagerErrors.PackageIdIsEmpty);
        }

        StShared.ConsoleWriteInformationLine(null, true, $"Check versions of tool {packageId}...");

        List<DotnetToolData>? installedTools = listOfToolsInstalled;
        if (installedTools == null)
        {
            OneOf<List<DotnetToolData>, Err[]> createListOfDotnetToolsInstalledResult =
                CreateListOfDotnetToolsInstalled();
            if (createListOfDotnetToolsInstalledResult.IsT1)
            {
                return Err.RecreateErrors(createListOfDotnetToolsInstalledResult.AsT1,
                    DotnetToolsManagerErrors.CreateListOfDotnetToolsInstalledError);
            }

            installedTools = createListOfDotnetToolsInstalledResult.AsT0;
        }

        OneOf<string, Err[]> getAvailableVersionOfToolResult = GetAvailableVersionOfTool(packageId);
        if (getAvailableVersionOfToolResult.IsT1)
        {
            return Err.RecreateErrors(getAvailableVersionOfToolResult.AsT1,
                DotnetToolsManagerErrors.GetAvailableVersionOfToolError);
        }

        string? availableVersion = getAvailableVersionOfToolResult.AsT0;

        DotnetToolData? nesTool = installedTools.FirstOrDefault(tool => tool.PackageId == packageId);

        string? installedVersion = nesTool is null ? "N/A" : nesTool.InstalledVersion;
        string? installedCommandName = nesTool?.CommandName;
        string? latestVersion = availableVersion ?? "N/A";

        bool haveChanges = false;

        if (!string.IsNullOrWhiteSpace(installedCommandName) && installedCommandName != dotnetToolData.CommandName)
        {
            haveChanges = true;
            dotnetToolData.CommandName = installedCommandName;
        }

        if (installedVersion != dotnetToolData.InstalledVersion)
        {
            haveChanges = true;
            dotnetToolData.InstalledVersion = installedVersion;
        }

        if (latestVersion == dotnetToolData.LatestVersion)
        {
            return haveChanges;
        }

        haveChanges = true;
        dotnetToolData.LatestVersion = latestVersion;

        return haveChanges;
    }

    private static OneOf<string, Err[]> GetAvailableVersionOfTool(string toolName)
    {
        var dotnetProcessor = new DotnetProcessor(null, false);
        OneOf<(string, int), Err[]> processResult = dotnetProcessor.SearchTool(toolName);
        return processResult.Match<OneOf<string, Err[]>>(t0 =>
        {
            string outputResult = t0.Item1;
            string[] outputLines = outputResult.Split(Environment.NewLine);
            if (outputLines.Length < 3)
            {
                return "N/A";
            }

            string[] lineParts = outputLines[2].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            return lineParts.Length < 2 ? "N/A" : lineParts[1];
        }, t1 => t1.ToArray());
    }

    private static OneOf<List<DotnetToolData>, Err[]> CreateListOfDotnetToolsInstalled()
    {
        var dotnetProcessor = new DotnetProcessor(null, false);
        OneOf<IEnumerable<string>, Err[]> getToolsRawListResult = dotnetProcessor.GetToolsRawList();
        return getToolsRawListResult.Match<OneOf<List<DotnetToolData>, Err[]>>(t0 =>
        {
            List<DotnetToolData> listOfTools = t0.Skip(2)
                .Select(line => line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                .Where(lineParts => lineParts.Length == 3).Select(lineParts => new DotnetToolData
                {
                    PackageId = lineParts[0],
                    InstalledVersion = lineParts[1],
                    LatestVersion = null,
                    CommandName = lineParts[2]
                }).ToList();

            return listOfTools;
        }, t1 => t1.ToArray());
    }
}
