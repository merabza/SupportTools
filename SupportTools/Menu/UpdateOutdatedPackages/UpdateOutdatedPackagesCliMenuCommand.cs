using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibGitWork.OutdatedPackages;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.UpdateOutdatedPackages;

public sealed class UpdateOutdatedPackagesCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Update Outdated Packages";

    private readonly Func<string, bool, bool> _inputBool;
    private readonly Func<string, string, string> _inputMessage;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly IOutdatedPackagesUpdateSteps _steps;

    public UpdateOutdatedPackagesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, parametersManager,
        new OutdatedPackagesUpdateSteps(logger, httpClientFactory, parametersManager),
        RemoteOnlyProjects.InputBoolFromConsole,
        (fieldName, defaultValue) => Inputer.InputTextRequired(fieldName, defaultValue))
    {
    }

    //კონსოლიდან შეყვანა და git-ისა და dotnet outdated-ის ნაბიჯები პარამეტრებადაა გამოტანილი, რომ ტესტებმა პასუხები
    //თვითონ მიაწოდონ და გარე პროგრამები არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal UpdateOutdatedPackagesCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        IOutdatedPackagesUpdateSteps steps, Func<string, bool, bool> inputBool,
        Func<string, string, string> inputMessage) : base("Update Outdated Packages", EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _steps = steps;
        _inputBool = inputBool;
        _inputMessage = inputMessage;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //remote პროექტები მხოლოდ ცხადი თანხმობით იკლონება (D3)
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        bool includeRemoteProjects = RemoteOnlyProjects.AskInclude(parameters.Projects, _inputBool);

        var updateOutdatedPackagesToolAction = UpdateOutdatedPackagesToolAction.Create(_logger, _parametersManager,
            _steps, null, null, includeRemoteProjects, true, _inputMessage, _inputBool, TimeProvider.System);
        return await updateOutdatedPackagesToolAction.Run(cancellationToken);
    }
}
