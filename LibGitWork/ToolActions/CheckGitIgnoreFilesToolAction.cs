using System;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace LibGitWork.ToolActions;

public sealed class CheckGitIgnoreFilesToolAction : GitToolAction
{
    private readonly IParametersManager _parametersManager;
    public const string ActionName = "Check .gitignore Files";

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckGitIgnoreFilesToolAction(ILogger logger, IParametersManager parametersManager) : base(logger,
        ActionName, null, null, true)
    {
        _parametersManager = parametersManager;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {

        var wrongGitignoreFilesListCreator = new WrongGitignoreFilesListCreator(Logger, _parametersManager);
        var wrongGitIgnoreFilesList = wrongGitignoreFilesListCreator.Create();

        if (wrongGitIgnoreFilesList.Count == 0)
        {
            Console.WriteLine("--wrong .gitignore files are not found");
            return Task.FromResult(true);
        }

        Console.WriteLine("wrong .gitignore files are found:");
        foreach (var (gitignoreFileName, _)in wrongGitIgnoreFilesList)
        {
            Console.WriteLine(gitignoreFileName);
        }

        return Task.FromResult(true);

    }
}