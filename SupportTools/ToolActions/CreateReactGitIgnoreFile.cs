using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;
using ToolsManagement.LibToolActions;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.ToolActions;

public sealed class CreateReactGitIgnoreFile : ToolAction
{
    private readonly string _gitIgnoreFileName;
    private readonly ILogger _logger;

    public CreateReactGitIgnoreFile(ILogger logger, string gitIgnoreFileName) : base(logger,
        nameof(CreateCSharpGitIgnoreFile), null, null)
    {
        _logger = logger;
        _gitIgnoreFileName = gitIgnoreFileName;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        const string code = """
                            # See https://help.github.com/articles/ignoring-files/ for more about ignoring files.

                            */src/appcarcass

                            # dependencies
                            */node_modules
                            */.pnp
                            .pnp.js

                            # testing
                            */coverage

                            # production
                            */build

                            # misc
                            .DS_Store
                            .env.local
                            .env.development.local
                            .env.test.local
                            .env.production.local

                            npm-debug.log*
                            yarn-debug.log*
                            yarn-error.log*


                            # Build results
                            [Dd]ebug/
                            [Dd]ebugPublic/
                            [Rr]elease/
                            [Rr]eleases/
                            x64/
                            x86/
                            build/
                            bld/
                            [Bb]in/
                            [Oo]bj/
                            [Oo]ut/
                            msbuild.log
                            msbuild.err
                            msbuild.wrn


                            # Visual Studio Code
                            .vscode
                            """;

        if (FileStat.CreatePrevFolderIfNotExists(_gitIgnoreFileName, true, _logger))
        {
            File.WriteAllText(_gitIgnoreFileName, code.Replace("\r\n", "\n"));
            return ValueTask.FromResult(true);
        }

        StShared.WriteErrorLine("File did not created", true);
        return ValueTask.FromResult(false);
    }
}