using System.IO;
using System.Threading;
using LibDataInput;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportTools.ToolActions;
using SupportToolsData.Models;

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
        const string cSharpGitIgnoreFileName = @"D:\1WorkSecurity\SupportTools\CSharp.gitignore";
        var createCSharpGitIgnoreFile = new CreateCSharpGitIgnoreFile(_logger, cSharpGitIgnoreFileName);
        var allSuccess = !TryAdd("CSharp", cSharpGitIgnoreFileName, createCSharpGitIgnoreFile);

        const string reactGitIgnoreFileName = @"D:\1WorkSecurity\SupportTools\React.gitignore";
        var createReactGitIgnoreFile = new CreateReactGitIgnoreFile(_logger, reactGitIgnoreFileName);
        if (TryAdd("React", reactGitIgnoreFileName, createReactGitIgnoreFile))
            allSuccess = false;

        const string defaultGitIgnoreFileName = @"D:\1WorkSecurity\SupportTools\Default.gitignore";
        var createDefaultGitIgnoreFile = new CreateDefaultGitIgnoreFile(_logger, defaultGitIgnoreFileName);
        if (TryAdd("Default", defaultGitIgnoreFileName, createDefaultGitIgnoreFile))
            allSuccess = false;
        return allSuccess;
    }

    private bool TryAdd(string recordName, string gitIgnoreFileFillName, ToolAction createGitIgnoreFileToolAction)
    {
        _parameters.GitIgnoreModelFilePaths.TryAdd(recordName, gitIgnoreFileFillName);
        if (File.Exists(gitIgnoreFileFillName) &&
            Inputer.InputBool($".gitignore file with name {gitIgnoreFileFillName} is already Exists. Regenerate?",
                false))
            return false;
        return createGitIgnoreFileToolAction.Run(CancellationToken.None).Result;
    }
}