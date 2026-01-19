using System.IO;
using LibDotnetWork;
using LibGitData;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.LibToolActions;

namespace LibGitWork.ToolActions;

public sealed class PackageUpdaterToolAction : ToolAction
{
    private const string CSharp = "CSharp";
    private readonly string _gitIgnorePathName;
    private readonly ILogger? _logger;
    private readonly string _projectFolderName;

    // ReSharper disable once ConvertToPrimaryConstructor
    private PackageUpdaterToolAction(ILogger? logger, GitSyncParameters gitSyncParameters) : base(logger, "Git Sync",
        null, null)
    {
        _logger = logger;
        _projectFolderName = Path.Combine(gitSyncParameters.GitsFolder, gitSyncParameters.GitData.GitProjectFolderName);
        _gitIgnorePathName = gitSyncParameters.GitData.GitIgnorePathName;
    }

    public static PackageUpdaterToolAction? Create(ILogger? logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol, string gitProjectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        var gitSyncParameters = GitSyncParameters.Create(loggerOrNull, supportToolsParameters, projectName, gitCol,
            gitProjectName, useConsole);

        if (gitSyncParameters is not null)
            return new PackageUpdaterToolAction(loggerOrNull, gitSyncParameters);

        StShared.WriteErrorLine("GitSyncParameters is not created", true);
        return null;
    }

    public void RunPackageUpdate()
    {
        //_projectFolderName

        if (_gitIgnorePathName != CSharp)
            return;

        var dotnetProcessor = new DotnetProcessor(_logger, true);

        var localResult = dotnetProcessor.UpdateOutdatedPackagesForProjectFolder(_projectFolderName);
        if (!localResult.IsT1)
            return;

        StShared.WriteErrorLine($"dotnet outdated finished with errors for {_projectFolderName})", true, _logger);
    }
}