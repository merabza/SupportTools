using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.ToolActions;

public sealed class ApplyEditorConfigPatternToProjectsWithoutPatternToolAction : ToolAction
{
    private readonly string _editorConfigPatternName;
    private readonly IParametersManager _parametersManager;

    public ApplyEditorConfigPatternToProjectsWithoutPatternToolAction(ILogger logger, string editorConfigPatternName,
        IParametersManager parametersManager) : base(logger,
        nameof(ApplyEditorConfigPatternToProjectsWithoutPatternToolAction), null, null)
    {
        _editorConfigPatternName = editorConfigPatternName;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //solution ფაილის გარეშე პროექტი არ მოწმდება, ამიტომ ასეთი პროექტი არ იცვლება
        bool isAnyChanged = false;
        foreach ((string _, ProjectModel project) in parameters.Projects.Where(x =>
                     string.IsNullOrWhiteSpace(x.Value.EditorConfigPatternName) &&
                     x.Value.EditorConfigFileName() is not null))
        {
            isAnyChanged = true;
            project.EditorConfigPatternName = _editorConfigPatternName;
        }

        if (isAnyChanged)
        {
            //შენახვა
            await _parametersManager.Save(parameters, "EditorConfigPatternNames applied success", null,
                cancellationToken);
        }
        else
        {
            StShared.WriteWarningLine("All Projects already have EditorConfigPatternNames, No Changes made", true);
        }

        return true;
    }
}
