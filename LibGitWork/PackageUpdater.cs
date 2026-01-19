using System.Collections.Generic;
using System.Linq;
using LibGitData;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibGitWork;

public sealed class PackageUpdater
{
    private readonly string _gitProjectName;
    private readonly ILogger? _logger;
    private readonly List<PackageUpdaterToolAction> _packageUpdaterToolActionList = [];
    private readonly ParametersManager _parametersManager;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PackageUpdater(ILogger? logger, ParametersManager parametersManager, string gitProjectName, bool useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _gitProjectName = gitProjectName;
        _useConsole = useConsole;
    }

    public int Count => _packageUpdaterToolActionList.Count;

    public void Add(string projectName, EGitCol gitCol)
    {
        var packageUpdaterToolAction = PackageUpdaterToolAction.Create(_logger, _parametersManager, projectName, gitCol,
            _gitProjectName, _useConsole);
        if (packageUpdaterToolAction is null)
            return;
        _packageUpdaterToolActionList.Add(packageUpdaterToolAction);
    }

    public void Run()
    {
        if (_packageUpdaterToolActionList.Count > 0)
            _packageUpdaterToolActionList.First().RunPackageUpdate();
    }
}