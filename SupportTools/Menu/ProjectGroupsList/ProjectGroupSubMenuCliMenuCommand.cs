using System;
using System.Globalization;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList;

public sealed class ProjectGroupSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupSubMenuCliMenuCommand(IServiceProvider serviceProvider, IParametersManager parametersManager,
        string projectGroupName, MenuParameters menuParameters) : base(projectGroupName, EMenuAction.LoadSubMenu)
    {
        _serviceProvider = serviceProvider;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
        _menuParameters = menuParameters;
    }

    public override CliMenuSet GetSubMenu()
    {
        _menuParameters.ProjectGroupName = _projectGroupName;
        return CliMenuSetFactory.CreateMenuSet(_projectGroupName, MenuData.ProjectGroupSubMenuCommandNames,
            _serviceProvider, _parametersManager);
    }

    protected override string GetStatus()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        return parameters.Projects.Count(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
            .ToString(CultureInfo.InvariantCulture);
    }
}
