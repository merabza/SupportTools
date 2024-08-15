using LibGitData;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace LibGitWork;

public class GitProjectSyncronizer
{
    private readonly List<GitSyncToolAction> _gitSyncToolActionList = [];
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _gitProjectName;

    public int Count =>_gitSyncToolActionList.Count;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectSyncronizer(ILogger? logger, ParametersManager parametersManager, string gitProjectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _gitProjectName = gitProjectName;
    }

    public void Add(string projectName, EGitCol gitCol)
    {
        var gitSyncToolAction = GitSyncToolAction.Create(_logger, _parametersManager, projectName, gitCol,
            _gitProjectName, true);
        if (gitSyncToolAction is null)
            return;
        _gitSyncToolActionList.Add(gitSyncToolAction);
    }

    public void RunSync()
    {
        foreach (var gitSyncToolAction in _gitSyncToolActionList)
        {
            var (phase1Result, gitProcessor) = gitSyncToolAction.RunActionPhase1();
            //აქ გავჩერდი, თუ საჭირო იყო, კლონირება მოხდა, დადგინდა ცვლილებები დასაკომიტებელია თუ არა. ცნობილია მოხდა თუ არა შეცდომები

            //თუ დაკომიტება საჭიროა დაკომიტდეს და დასინქრონიზდეს.

            //თუ დაკომიტება საჭირო არაა დადგინდეს საჭიროა თუ არა სინქრონიზაცია და საჭიროების შემთხვევაში დასინქრონიზდეს.
            
        }

    }

}