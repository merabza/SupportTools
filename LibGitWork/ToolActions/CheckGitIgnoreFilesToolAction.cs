using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibGitWork.ToolActions;

public sealed class CheckGitIgnoreFilesToolAction : GitToolAction
{
    public const string ActionName = "Check .gitignore Files";
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckGitIgnoreFilesToolAction(ILogger logger, IParametersManager parametersManager, bool useConsole) : base(
        logger, ActionName, null, null, useConsole)
    {
        _parametersManager = parametersManager;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var wrongGitignoreFilesListCreator = new WrongGitignoreFilesListCreator(Logger, _parametersManager, UseConsole);
        Dictionary<string, string> wrongGitIgnoreFilesList = wrongGitignoreFilesListCreator.Create();

        if (wrongGitIgnoreFilesList.Count == 0)
        {
            Console.WriteLine("--wrong .gitignore files are not found");
            return ValueTask.FromResult(true);
        }

        Console.WriteLine("wrong .gitignore files are found:");
        foreach ((string gitignoreFileName, string _)in wrongGitIgnoreFilesList)
        {
            Console.WriteLine(gitignoreFileName);
        }

        return ValueTask.FromResult(true);
    }
}
