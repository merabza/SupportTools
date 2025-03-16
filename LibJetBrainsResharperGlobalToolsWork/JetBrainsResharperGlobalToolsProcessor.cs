using System.Collections.Generic;
using LanguageExt;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace LibJetBrainsResharperGlobalToolsWork;

public class JetBrainsResharperGlobalToolsProcessor
{
    private const string Jb = "jb";
    private readonly ILogger? _logger;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public JetBrainsResharperGlobalToolsProcessor(ILogger? logger, bool useConsole)
    {
        _logger = logger;
        _useConsole = useConsole;
    }

    public Option<IEnumerable<Err>> Cleanupcode(string path, bool includeJson = false)
    {
        return StShared.RunProcess(_useConsole, _logger, Jb,
            $"cleanupcode{(includeJson ? "" : " --exclude=\"**.json\"")} {path}");
    }
}