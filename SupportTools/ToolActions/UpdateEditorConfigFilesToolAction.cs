using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Tools;
using SystemTools.BackgroundTasks;

namespace SupportTools.ToolActions;

public sealed class UpdateEditorConfigFilesToolAction : ToolAction
{
    public const string ActionName = "Update .editorconfig Files";
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateEditorConfigFilesToolAction(ILogger logger, IParametersManager parametersManager, bool useConsole) :
        base(logger, ActionName, null, null, useConsole)
    {
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var wrongEditorConfigFilesListCreator = new WrongEditorConfigFilesListCreator(Logger, _parametersManager);
        Dictionary<string, string> wrongEditorConfigFilesList = wrongEditorConfigFilesListCreator.Create();

        if (wrongEditorConfigFilesList.Count == 0)
        {
            Console.WriteLine("--wrong .editorconfig files are not found");
            return true;
        }

        Console.WriteLine("Update wrong .editorconfig files");
        foreach ((string editorConfigFileName, string editorConfigFileContent) in wrongEditorConfigFilesList)
        {
            Console.WriteLine($"Update {editorConfigFileName}");
            await File.WriteAllTextAsync(editorConfigFileName, editorConfigFileContent, cancellationToken);
        }

        return true;
    }
}
