using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData;

namespace SupportTools.CliMenuCommands;

public sealed class TemplateSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _templateName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TemplateSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string templateName) : base(templateName, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _templateName = templateName;
        _httpClientFactory = httpClientFactory;
    }

    public override CliMenuSet GetSubMenu()
    {
        var templateSubMenuSet = new CliMenuSet($"Template => {_templateName}");

        //პროექტის წაშლა
        var deleteTemplateCommand = new DeleteTemplateCliMenuCommand(_parametersManager, _templateName);
        templateSubMenuSet.AddMenuItem(deleteTemplateCommand);

        //პროექტის პარამეტრი
        var templateCruder = TemplateCruder.Create(_logger, _parametersManager);
        var editCommand = new EditItemAllFieldsInSequenceCliMenuCommand(templateCruder, _templateName);
        templateSubMenuSet.AddMenuItem(editCommand);

        templateCruder.FillDetailsSubMenu(templateSubMenuSet, _templateName);

        templateSubMenuSet.AddMenuItem(new TemplateRunCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _templateName, ETestOrReal.Test));

        templateSubMenuSet.AddMenuItem(new TemplateRunCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _templateName, ETestOrReal.Real));

        //მთავარ მენიუში გასვლა
        templateSubMenuSet.AddEscapeCommand();

        return templateSubMenuSet;
    }
}
