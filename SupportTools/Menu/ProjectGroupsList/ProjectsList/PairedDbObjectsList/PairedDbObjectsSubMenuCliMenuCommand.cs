using System;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class PairedDbObjectsSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedDbObjectsSubMenuCliMenuCommand(IServiceProvider serviceProvider, IParametersManager parametersManager,
        string projectName, SupportToolsMenuParameters menuParameters) : base("Paired Db Objects",
        EMenuAction.LoadSubMenu)
    {
        _serviceProvider = serviceProvider;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _menuParameters = menuParameters;
    }

    public override CliMenuSet? GetSubMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} not found", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName))
        {
            StShared.WriteErrorLine(
                $"Project {_projectName} does not contain PairedDbObjectsResultFileName. Please set it in project parameters.",
                true);
            return null;
        }

        _menuParameters.ProjectName = _projectName;
        _menuParameters.PairedTableKey = null;
        _menuParameters.PairedFieldKey = null;

        return CliMenuSetFactory.CreateMenuSet($"{_projectName} => Paired Db Objects",
            MenuData.PairedDbObjectsSubMenuCommandFactoryStrategyNames, _serviceProvider);
    }
}
