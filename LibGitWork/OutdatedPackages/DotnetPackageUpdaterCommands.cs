using LibDotnetWork;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;

namespace LibGitWork.OutdatedPackages;

internal sealed class DotnetPackageUpdaterCommands : IPackageUpdaterCommands
{
    private readonly DotnetProcessor _dotnetProcessor;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetPackageUpdaterCommands(ILogger? logger)
    {
        _dotnetProcessor = new DotnetProcessor(logger, true);
    }

    public Result RestoreForOutdated(string solutionFileFullName)
    {
        return _dotnetProcessor.RestoreForOutdated(solutionFileFullName);
    }

    public Result<(string, int)> UpdateOutdatedPackages(string projectFolderName)
    {
        return _dotnetProcessor.UpdateOutdatedPackagesForProjectFolder(projectFolderName, false);
    }

    public Result RestoreAfterOutdated(string solutionFileFullName)
    {
        return _dotnetProcessor.RestoreAfterOutdated(solutionFileFullName);
    }
}
