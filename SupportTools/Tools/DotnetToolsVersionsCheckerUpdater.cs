using System;
using System.Collections.Generic;
using System.Linq;
using LibDotnetWork;
using LibParameters;
using OneOf;
using SupportTools.Errors;
using SupportToolsData.Models;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace SupportTools.Tools;

public static class DotnetToolsVersionsCheckerUpdater
{
    public static bool Check(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        StShared.ConsoleWriteInformationLine(null, true, "Checking versions for all tools...");
        var checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
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
        if (!parameters.DotnetTools.ContainsKey(toolKey))
        {
            StShared.WriteErrorLine($"Tool with key {toolKey} not found.", true);
            return false;
        }

        StShared.ConsoleWriteInformationLine(null, true, "Checking versions for tool {0}...", toolKey);
        var checkVersionsForOneToolResult = CheckVersionsForOneTool(parameters.DotnetTools[toolKey], null);
        if (checkVersionsForOneToolResult.IsT0)
            return true;
        Err.PrintErrorsOnConsole(checkVersionsForOneToolResult.AsT1);
        return false;
    }

    public static bool UpdateOne(IParametersManager parametersManager, string toolKey)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        if (!parameters.DotnetTools.TryGetValue(toolKey, out var dotnetTool))
        {
            StShared.WriteErrorLine($"Tool with key {toolKey} not found.", true);
            return false;
        }

        var checkVersionsForOneToolResult = CheckVersionsForOneTool(dotnetTool, null);
        if (checkVersionsForOneToolResult.IsT1)
        {
            Err.PrintErrorsOnConsole(checkVersionsForOneToolResult.AsT1);
            return false;
        }

        if (!checkVersionsForOneToolResult.AsT0)
            return true;

        var updateOneToolToLatestVersionResult = UpdateOneToolToLatestVersion(dotnetTool);
        if (updateOneToolToLatestVersionResult.IsT1)
        {
            Err.PrintErrorsOnConsole(updateOneToolToLatestVersionResult.AsT1);
            return false;
        }

        checkVersionsForOneToolResult = CheckVersionsForOneTool(dotnetTool, null);
        if (!checkVersionsForOneToolResult.IsT1)
            return true;
        Err.PrintErrorsOnConsole(checkVersionsForOneToolResult.AsT1);
        return false;
    }

    public static bool UpdateAllToolsToLatestVersion(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        StShared.ConsoleWriteInformationLine(null, true, "Checking for tools Updates...");
        var checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
        if (checkVersionsForAllToolsResult.IsT1)
        {
            Err.PrintErrorsOnConsole(checkVersionsForAllToolsResult.AsT1);
            return false;
        }

        var dotnetTools = parameters.DotnetTools;

        var atLeastOneUpdatedOrInstalled = false;
        foreach (var updateOneToolToLatestVersionResult in dotnetTools.Select(kvp =>
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

    private static OneOf<bool, IEnumerable<Err>> UpdateOneToolToLatestVersion(DotnetToolData dotnetToolData)
    {
        if (string.IsNullOrWhiteSpace(dotnetToolData.PackageId) ||
            string.IsNullOrWhiteSpace(dotnetToolData.LatestVersion) || dotnetToolData.LatestVersion == "N/A" ||
            dotnetToolData.InstalledVersion == dotnetToolData.LatestVersion)
            return false;
        var toolInstalled = dotnetToolData.InstalledVersion != "N/A";
        var command = toolInstalled ? "update" : "install";
        StShared.ConsoleWriteInformationLine(null, true, "{0}ing {1}...", command, dotnetToolData.PackageId);

        var dotnetProcessor = new DotnetProcessor(null, false);
        var result = toolInstalled
            ? dotnetProcessor.UpdateTool(dotnetToolData.PackageId)
            : dotnetProcessor.InstallTool(dotnetToolData.PackageId);
        return result.Match<OneOf<bool, IEnumerable<Err>>>(some => (Err[])some, true);
    }

    private static OneOf<bool, IEnumerable<Err>> CheckVersionsForAllTools(
        Dictionary<string, DotnetToolData> necessaryDotnetTools)
    {
        StShared.ConsoleWriteInformationLine(null, true, "Create List of Installed tools...");
        var createListOfDotnetToolsInstalledResult = CreateListOfDotnetToolsInstalled();
        if (createListOfDotnetToolsInstalledResult.IsT1)
            return Err.RecreateErrors(createListOfDotnetToolsInstalledResult.AsT1,
                DotnetToolsManagerErrors.CreateListOfDotnetToolsInstalledError);
        var listOfToolsInstalled = createListOfDotnetToolsInstalledResult.AsT0;

        List<Err> errors = [];
        var madeChanges = false;

        foreach (var kvp in necessaryDotnetTools)
        {
            var checkVersionsForOneToolResult = CheckVersionsForOneTool(kvp.Value, listOfToolsInstalled);
            if (checkVersionsForOneToolResult.IsT1)
                errors.AddRange(Err.RecreateErrors(checkVersionsForOneToolResult.AsT1,
                    DotnetToolsManagerErrors.CheckVersionsForOneToolError(kvp.Key)));
            madeChanges = checkVersionsForOneToolResult.AsT0;
        }

        if (errors.Count > 0)
            return errors;
        return madeChanges;
    }

    private static OneOf<bool, IEnumerable<Err>> CheckVersionsForOneTool(DotnetToolData dotnetToolData,
        List<DotnetToolData>? listOfToolsInstalled)
    {
        var packageId = dotnetToolData.PackageId;
        if (string.IsNullOrEmpty(packageId))
            return Err.CreateArr(DotnetToolsManagerErrors.PackageIdIsEmpty);

        StShared.ConsoleWriteInformationLine(null, true, $"Check versions of tool {packageId}...");

        var installedTools = listOfToolsInstalled;
        if (installedTools == null)
        {
            var createListOfDotnetToolsInstalledResult = CreateListOfDotnetToolsInstalled();
            if (createListOfDotnetToolsInstalledResult.IsT1)
                return Err.RecreateErrors(createListOfDotnetToolsInstalledResult.AsT1,
                    DotnetToolsManagerErrors.CreateListOfDotnetToolsInstalledError);
            installedTools = createListOfDotnetToolsInstalledResult.AsT0;
        }

        var getAvailableVersionOfToolResult = GetAvailableVersionOfTool(packageId);
        if (getAvailableVersionOfToolResult.IsT1)
            return Err.RecreateErrors(getAvailableVersionOfToolResult.AsT1,
                DotnetToolsManagerErrors.GetAvailableVersionOfToolError);

        var availableVersion = getAvailableVersionOfToolResult.AsT0;

        var nesTool = installedTools.FirstOrDefault(tool => tool.PackageId == packageId);

        var installedVersion = nesTool is null ? "N/A" : nesTool.InstalledVersion;
        var installedCommandName = nesTool?.CommandName;
        var latestVersion = availableVersion ?? "N/A";

        var haveChanges = false;

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
            return haveChanges;

        haveChanges = true;
        dotnetToolData.LatestVersion = latestVersion;

        return haveChanges;
    }

    private static OneOf<string, IEnumerable<Err>> GetAvailableVersionOfTool(string toolName)
    {
        var dotnetProcessor = new DotnetProcessor(null, false);
        var processResult = dotnetProcessor.SearchTool(toolName);
        return processResult.Match<OneOf<string, IEnumerable<Err>>>(t0 =>
        {
            var outputResult = t0.Item1;
            var outputLines = outputResult.Split(Environment.NewLine);
            if (outputLines.Length < 3) return "N/A";
            var lineParts = outputLines[2].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            return lineParts.Length < 2 ? "N/A" : lineParts[1];
        }, t1 => t1.ToArray());
    }

    private static OneOf<List<DotnetToolData>, IEnumerable<Err>> CreateListOfDotnetToolsInstalled()
    {
        var dotnetProcessor = new DotnetProcessor(null, false);
        var getToolsRawListResult = dotnetProcessor.GetToolsRawList();
        return getToolsRawListResult.Match<OneOf<List<DotnetToolData>, IEnumerable<Err>>>(t0 =>
        {
            var listOfTools = t0.Skip(2).Select(line => line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
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