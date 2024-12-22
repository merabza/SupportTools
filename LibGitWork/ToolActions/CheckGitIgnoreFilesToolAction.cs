using System;
using System.Threading;
using System.Threading.Tasks;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibGitWork.ToolActions;

public sealed class CheckGitIgnoreFilesToolAction : GitToolAction
{
    public const string ActionName = "Check .gitignore Files";
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckGitIgnoreFilesToolAction(ILogger logger, IParametersManager parametersManager, bool useConsole) : base(
        logger,
        ActionName, null, null, useConsole)
    {
        _parametersManager = parametersManager;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var wrongGitignoreFilesListCreator = new WrongGitignoreFilesListCreator(Logger, _parametersManager, UseConsole);
        var wrongGitIgnoreFilesList = wrongGitignoreFilesListCreator.Create();

        if (wrongGitIgnoreFilesList.Count == 0)
        {
            Console.WriteLine("--wrong .gitignore files are not found");
            return ValueTask.FromResult(true);
        }

        Console.WriteLine("wrong .gitignore files are found:");
        foreach (var (gitignoreFileName, _)in wrongGitIgnoreFilesList) Console.WriteLine(gitignoreFileName);

        return ValueTask.FromResult(true);
    }
}