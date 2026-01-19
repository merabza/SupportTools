using System;
using System.Net.Http;
using System.Threading;
using AppCliTools.CliMenu;
using LibAppProjectCreator.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateUpdateFrontSpaProjectCliMenuCommand : CliMenuCommand
{
    private readonly ReCreateUpdateFrontSpaProjectToolAction _reCreateUpdateFrontSpaProjectToolAction;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReCreateUpdateFrontSpaProjectCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base("ReCreate Update Front Spa Project", EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _reCreateUpdateFrontSpaProjectToolAction =
            new ReCreateUpdateFrontSpaProjectToolAction(logger, httpClientFactory, parametersManager, true);
    }

    protected override string GetActionDescription()
    {
        return AppProjectCreatorByTemplateToolAction.ActionDescription;
    }

    protected override bool RunBody()
    {
        var projectName = MenuSet?.ParentMenu?.Caption;
        if (string.IsNullOrEmpty(projectName))
        {
            Console.WriteLine("Project name is not set in menu caption.");
            return false;
        }

        _reCreateUpdateFrontSpaProjectToolAction.SetProjectName(projectName);
        return _reCreateUpdateFrontSpaProjectToolAction.Run(CancellationToken.None).Result;
    }
}