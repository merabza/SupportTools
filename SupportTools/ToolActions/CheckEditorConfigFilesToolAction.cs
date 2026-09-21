using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Tools;
using SystemTools.BackgroundTasks;

namespace SupportTools.ToolActions;

public sealed class CheckEditorConfigFilesToolAction : ToolAction
{
    public const string ActionName = "Check .editorconfig Files";
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckEditorConfigFilesToolAction(ILogger logger, IParametersManager parametersManager, bool useConsole) :
        base(logger, ActionName, null, null, useConsole)
    {
        _parametersManager = parametersManager;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var wrongEditorConfigFilesListCreator = new WrongEditorConfigFilesListCreator(Logger, _parametersManager);
        Dictionary<string, string> wrongEditorConfigFilesList = wrongEditorConfigFilesListCreator.Create();

        if (wrongEditorConfigFilesList.Count == 0)
        {
            Console.WriteLine("--wrong .editorconfig files are not found");
            return ValueTask.FromResult(true);
        }

        Console.WriteLine("wrong .editorconfig files are found:");
        foreach ((string editorConfigFileName, _) in wrongEditorConfigFilesList)
        {
            Console.WriteLine(editorConfigFileName);
        }

        return ValueTask.FromResult(true);
    }
}
