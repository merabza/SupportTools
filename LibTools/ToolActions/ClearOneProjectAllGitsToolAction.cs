using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibGitWork.Models;
using LibTools.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.LibToolActions;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibTools.ToolActions;

public sealed class ClearOneProjectAllGitsToolAction : ToolAction
{
    private readonly ClearOneProjectAllGitsParameters _clearOneProjectAllGitsParameters;
    private readonly string? _excludeFolder;
    private readonly ILogger? _logger;

    private ClearOneProjectAllGitsToolAction(ILogger? logger,
        ClearOneProjectAllGitsParameters clearOneProjectAllGitsParameters, string? excludeFolder) : base(logger,
        "Clear One Project All Gits", null, null)
    {
        _logger = logger;
        _clearOneProjectAllGitsParameters = clearOneProjectAllGitsParameters;
        _excludeFolder = excludeFolder;
    }

    public static ClearOneProjectAllGitsToolAction? Create(ILogger? logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol, string? excludeFolder)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        var clearOneProjectAllGitsParameters =
            ClearOneProjectAllGitsParameters.Create(loggerOrNull, supportToolsParameters, projectName, gitCol);

        if (clearOneProjectAllGitsParameters is not null)
        {
            return new ClearOneProjectAllGitsToolAction(loggerOrNull, clearOneProjectAllGitsParameters, excludeFolder);
        }

        StShared.WriteErrorLine("ClearOneProjectAllGitsParameters is not created", true);
        return null;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        foreach (GitData gitData in _clearOneProjectAllGitsParameters.GitData
                     .Where(x => x.GitIgnorePathName == "CSharp").OrderBy(x => x.GitProjectFolderName))
        {
            var gitClear = new GitClearToolAction(_logger,
                new GitClearParameters(gitData, _clearOneProjectAllGitsParameters.GitsFolder), _excludeFolder);
            await gitClear.Run(cancellationToken);
        }

        return true;
    }
}
