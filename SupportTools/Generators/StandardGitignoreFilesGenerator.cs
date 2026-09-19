using System.IO;
using System.Threading;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using SupportTools.ToolActions;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;

namespace SupportTools.Generators;

public sealed class StandardGitignoreFilesGenerator
{
    private readonly ILogger _logger;
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public StandardGitignoreFilesGenerator(ILogger logger, SupportToolsParameters parameters)
    {
        _logger = logger;
        _parameters = parameters;
    }

    public bool Generate()
    {
        if (string.IsNullOrWhiteSpace(_parameters.FolderForGitignoreFiles))
        {
            _logger.LogError("supportToolsParameters.FolderForGitignoreFiles is empty");
            return false;
        }

        string cSharpGitIgnoreFileName =
            SupportToolsParameters.GetGitIgnoreModelFilePath(_parameters.FolderForGitignoreFiles, "CSharp");
        var createCSharpGitIgnoreFile = new CreateCSharpGitIgnoreFile(_logger, cSharpGitIgnoreFileName);
        bool allSuccess = TryAdd("CSharp", cSharpGitIgnoreFileName, createCSharpGitIgnoreFile);

        string reactGitIgnoreFileName =
            SupportToolsParameters.GetGitIgnoreModelFilePath(_parameters.FolderForGitignoreFiles, "React");
        var createReactGitIgnoreFile = new CreateReactGitIgnoreFile(_logger, reactGitIgnoreFileName);
        if (!TryAdd("React", reactGitIgnoreFileName, createReactGitIgnoreFile))
        {
            allSuccess = false;
        }

        string defaultGitIgnoreFileName =
            SupportToolsParameters.GetGitIgnoreModelFilePath(_parameters.FolderForGitignoreFiles, "Default");
        var createDefaultGitIgnoreFile = new CreateDefaultGitIgnoreFile(_logger, defaultGitIgnoreFileName);
        if (!TryAdd("Default", defaultGitIgnoreFileName, createDefaultGitIgnoreFile))
        {
            allSuccess = false;
        }

        return allSuccess;
    }

    private bool TryAdd(string recordName, string gitIgnoreFileFillName, ToolAction createGitIgnoreFileToolAction)
    {
        if (!_parameters.GitIgnoreModels.Contains(recordName))
        {
            _parameters.GitIgnoreModels.Add(recordName);
        }

        if (File.Exists(gitIgnoreFileFillName) &&
            !Inputer.InputBool($".gitignore file with name {gitIgnoreFileFillName} is already Exists. Regenerate?",
                false))
        {
            //არსებული ფაილის თავიდან შექმნაზე უარი შეცდომა არ არის
            return true;
        }

        return createGitIgnoreFileToolAction.Run(CancellationToken.None).Result;
    }
}
