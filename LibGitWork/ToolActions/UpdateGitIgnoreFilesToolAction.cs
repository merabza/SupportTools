using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.BackgroundTasks;

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

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var wrongGitignoreFilesListCreator = new WrongGitignoreFilesListCreator(Logger, _parametersManager, UseConsole);
        Dictionary<string, (string, string)> wrongGitIgnoreFilesList = wrongGitignoreFilesListCreator.Create();

        if (wrongGitIgnoreFilesList.Count == 0)
        {
            Console.WriteLine("--wrong .gitignore files are not found");
            return true;
        }

        Console.WriteLine("Update wrong .gitignore files");
        List<string> changedGitProjectsList = [];
        foreach ((string gitignoreFileName, (string gitignoreFileContent, string gitProjectName)) in
                 wrongGitIgnoreFilesList)
        {
            if (changedGitProjectsList.Contains(gitProjectName))
            {
                continue;
            }

            Console.WriteLine($"Update {gitignoreFileName}");
            await File.WriteAllTextAsync(gitignoreFileName, gitignoreFileContent, cancellationToken);
            changedGitProjectsList.Add(gitProjectName);
        }

        return true;
    }
}
