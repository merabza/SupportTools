using System;
using System.Collections.Generic;
using System.Linq;
using LibDotnetWork;
using ParametersManagement.LibParameters;
using SupportTools.Errors;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.Tools;

public static class DotnetToolsVersionsCheckerUpdater
{
    public static bool Check(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        StShared.ConsoleWriteInformationLine(null, true, "Checking versions for all tools...");
        Result<bool> checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
        StShared.ConsoleWriteInformationLine(null, true, "Checking versions for all tools Finished.");

        if (checkVersionsForAllToolsResult.IsSuccess)
        {
            return checkVersionsForAllToolsResult.Value;
        }

        checkVersionsForAllToolsResult.Error.PrintErrorsOnConsole();
        return false;
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
        Result<bool> checkVersionsForOneToolResult = CheckVersionsForOneTool(value, null);
        if (checkVersionsForOneToolResult.IsSuccess)
        {
            return true;
        }

        checkVersionsForOneToolResult.Error.PrintErrorsOnConsole();
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

        Result<bool> checkVersionsForOneToolResult = CheckVersionsForOneTool(dotnetTool, null);
        if (checkVersionsForOneToolResult.IsFailure)
        {
            checkVersionsForOneToolResult.Error.PrintErrorsOnConsole();
            return false;
        }

        if (!checkVersionsForOneToolResult.Value)
        {
            return true;
        }

        Result<bool> updateOneToolToLatestVersionResult = UpdateOneToolToLatestVersion(dotnetTool);
        if (updateOneToolToLatestVersionResult.IsFailure)
        {
            updateOneToolToLatestVersionResult.Error.PrintErrorsOnConsole();
            return false;
        }

        checkVersionsForOneToolResult = CheckVersionsForOneTool(dotnetTool, null);
        if (checkVersionsForOneToolResult.IsSuccess)
        {
            return true;
        }

        checkVersionsForOneToolResult.Error.PrintErrorsOnConsole();
        return false;
    }

    public static bool UpdateAllToolsToLatestVersion(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        StShared.ConsoleWriteInformationLine(null, true, "Checking for tools Updates...");
        Result<bool> checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
        if (checkVersionsForAllToolsResult.IsFailure)
        {
            checkVersionsForAllToolsResult.Error.PrintErrorsOnConsole();
            return false;
        }

        Dictionary<string, DotnetToolData> dotnetTools = parameters.DotnetTools;

        bool atLeastOneUpdatedOrInstalled = false;
        foreach (Result<bool> updateOneToolToLatestVersionResult in dotnetTools.Select(kvp =>
                     UpdateOneToolToLatestVersion(kvp.Value)))
        {
            if (updateOneToolToLatestVersionResult.IsFailure)
            {
                updateOneToolToLatestVersionResult.Error.PrintErrorsOnConsole();
                return false;
            }

            atLeastOneUpdatedOrInstalled = updateOneToolToLatestVersionResult.Value || atLeastOneUpdatedOrInstalled;
        }

        if (atLeastOneUpdatedOrInstalled)
        {
            StShared.ConsoleWriteInformationLine(null, true, "Updating tools List...");
            checkVersionsForAllToolsResult = CheckVersionsForAllTools(parameters.DotnetTools);
            if (checkVersionsForAllToolsResult.IsFailure)
            {
                checkVersionsForAllToolsResult.Error.PrintErrorsOnConsole();
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

    private static Result<bool> UpdateOneToolToLatestVersion(DotnetToolData dotnetToolData)
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
        Result result = toolInstalled
            ? dotnetProcessor.UpdateTool(dotnetToolData.PackageId, dotnetToolData.MaxVersion)
            : dotnetProcessor.InstallTool(dotnetToolData.PackageId, dotnetToolData.MaxVersion);
        if (result.IsFailure)
        {
            return result.Error;
        }

        return true;
    }

    private static Result<bool> CheckVersionsForAllTools(Dictionary<string, DotnetToolData> necessaryDotnetTools)
    {
        StShared.ConsoleWriteInformationLine(null, true, "Create List of Installed tools...");
        Result<List<DotnetToolData>> createListOfDotnetToolsInstalledResult = CreateListOfDotnetToolsInstalled();
        if (createListOfDotnetToolsInstalledResult.IsFailure)
        {
            return Result.CreateValidationError([
                .. createListOfDotnetToolsInstalledResult.Error.ToErrorArray(),
                DotnetToolsManagerErrors.CreateListOfDotnetToolsInstalledError
            ]);
        }

        List<DotnetToolData> listOfToolsInstalled = createListOfDotnetToolsInstalledResult.Value;

        List<Error> errors = [];
        bool madeChanges = false;

        foreach (KeyValuePair<string, DotnetToolData> kvp in necessaryDotnetTools)
        {
            Result<bool> checkVersionsForOneToolResult = CheckVersionsForOneTool(kvp.Value, listOfToolsInstalled);
            if (checkVersionsForOneToolResult.IsFailure)
            {
                errors.AddRange(checkVersionsForOneToolResult.Error.ToErrorArray());
                errors.Add(DotnetToolsManagerErrors.CheckVersionsForOneToolError(kvp.Key));
                continue;
            }

            madeChanges = checkVersionsForOneToolResult.Value;
        }

        if (errors.Count > 0)
        {
            return Result.CreateValidationError([.. errors]);
        }

        return madeChanges;
    }

    private static Result<bool> CheckVersionsForOneTool(DotnetToolData dotnetToolData,
        List<DotnetToolData>? listOfToolsInstalled)
    {
        string? packageId = dotnetToolData.PackageId;
        if (string.IsNullOrEmpty(packageId))
        {
            return DotnetToolsManagerErrors.PackageIdIsEmpty;
        }

        StShared.ConsoleWriteInformationLine(null, true, $"Check versions of tool {packageId}...");

        List<DotnetToolData>? installedTools = listOfToolsInstalled;
        if (installedTools == null)
        {
            Result<List<DotnetToolData>> createListOfDotnetToolsInstalledResult = CreateListOfDotnetToolsInstalled();
            if (createListOfDotnetToolsInstalledResult.IsFailure)
            {
                return Result.CreateValidationError([
                    .. createListOfDotnetToolsInstalledResult.Error.ToErrorArray(),
                    DotnetToolsManagerErrors.CreateListOfDotnetToolsInstalledError
                ]);
            }

            installedTools = createListOfDotnetToolsInstalledResult.Value;
        }

        Result<string> getAvailableVersionOfToolResult = GetAvailableVersionOfTool(packageId);
        if (getAvailableVersionOfToolResult.IsFailure)
        {
            return Result.CreateValidationError([
                .. getAvailableVersionOfToolResult.Error.ToErrorArray(),
                DotnetToolsManagerErrors.GetAvailableVersionOfToolError
            ]);
        }

        string? availableVersion = getAvailableVersionOfToolResult.Value;

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

    private static Result<string> GetAvailableVersionOfTool(string toolName)
    {
        var dotnetProcessor = new DotnetProcessor(null, false);
        Result<(string, int)> processResult = dotnetProcessor.SearchTool(toolName);
        if (processResult.IsFailure)
        {
            return processResult.Error;
        }

        string outputResult = processResult.Value.Item1;
        string[] outputLines = outputResult.Split(Environment.NewLine);
        if (outputLines.Length < 3)
        {
            return "N/A";
        }

        string[] lineParts = outputLines[2].Split(" ", StringSplitOptions.RemoveEmptyEntries);
        return lineParts.Length < 2 ? "N/A" : lineParts[1];
    }

    private static Result<List<DotnetToolData>> CreateListOfDotnetToolsInstalled()
    {
        var dotnetProcessor = new DotnetProcessor(null, false);
        Result<IEnumerable<string>> getToolsRawListResult = dotnetProcessor.GetToolsRawList();
        if (getToolsRawListResult.IsFailure)
        {
            return getToolsRawListResult.Error;
        }

        List<DotnetToolData> listOfTools =
        [
            .. getToolsRawListResult.Value.Skip(2).Select(line => line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                .Where(lineParts => lineParts.Length == 3).Select(lineParts => new DotnetToolData
                {
                    PackageId = lineParts[0],
                    InstalledVersion = lineParts[1],
                    LatestVersion = null,
                    CommandName = lineParts[2]
                })
        ];

        return listOfTools;
    }
}
