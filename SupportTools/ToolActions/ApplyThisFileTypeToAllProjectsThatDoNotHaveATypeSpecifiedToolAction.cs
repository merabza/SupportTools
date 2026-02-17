using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.LibToolActions;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.ToolActions;

public sealed class ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedToolAction : ToolAction
{
    private readonly string _gitIgnoreFileName;
    private readonly IParametersManager _parametersManager;

    public ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedToolAction(ILogger logger, string gitIgnoreFileName,
        IParametersManager parametersManager) : base(logger,
        nameof(ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedToolAction), null, null)
    {
        _gitIgnoreFileName = gitIgnoreFileName;
        _parametersManager = parametersManager;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        bool isAnyChanged = false;
        foreach ((string _, GitDataModel git) in parameters.Gits.Where(x => x.Value.GitIgnorePathName is null))
        {
            isAnyChanged = true;
            git.GitIgnorePathName = _gitIgnoreFileName;
        }

        if (isAnyChanged)
            //შენახვა
        {
            _parametersManager.Save(parameters, "GitIgnorePathNames applied success");
        }
        else
        {
            StShared.WriteWarningLine("All Git Projects already have GitIgnorePathNames, No Changes made", true);
        }

        return ValueTask.FromResult(true);
    }
}
