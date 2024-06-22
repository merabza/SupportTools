using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibGitWork.ToolActions;

public sealed class UpdateGitIgnoreFilesToolAction : ToolAction
{
    public const string ActionName = "Update .gitignore Files";
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitIgnoreFilesToolAction(ILogger logger, IParametersManager parametersManager, bool useConsole) : base(
        logger, ActionName, null, null, useConsole)
    {
        _parametersManager = parametersManager;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var wrongGitignoreFilesListCreator = new WrongGitignoreFilesListCreator(Logger, _parametersManager, UseConsole);
        var wrongGitIgnoreFilesList = wrongGitignoreFilesListCreator.Create();

        if (wrongGitIgnoreFilesList.Count == 0)
        {
            Console.WriteLine("--wrong .gitignore files are not found");
            return Task.FromResult(true);
        }

        Console.WriteLine("Update wrong .gitignore files");
        foreach (var (gitignoreFileName, gitignoreFileContent)in wrongGitIgnoreFilesList)
        {
            Console.WriteLine($"Update {gitignoreFileName}");
            File.WriteAllText(gitignoreFileName, gitignoreFileContent);
        }

        return Task.FromResult(true);
    }
}