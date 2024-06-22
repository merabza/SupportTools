using System.Threading;
using CliMenu;
using LibAppProjectCreator;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;

namespace SupportTools.CliMenuCommands;

public sealed class TemplateRunCliMenuCommand : CliMenuCommand
{
    private readonly AppProjectCreatorByTemplateToolAction _appProjectCreatorByTemplate;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TemplateRunCliMenuCommand(ILogger logger, ParametersManager parametersManager, string templateName,
        ETestOrReal testOrReal) : base(null, EMenuAction.Reload)
    {
        _appProjectCreatorByTemplate =
            new AppProjectCreatorByTemplateToolAction(logger, parametersManager, templateName, testOrReal, true);
    }

    protected override string GetActionDescription()
    {
        return AppProjectCreatorByTemplateToolAction.ActionDescription;
    }

    protected override bool RunBody()
    {
        return _appProjectCreatorByTemplate.Run(CancellationToken.None).Result;
    }
}