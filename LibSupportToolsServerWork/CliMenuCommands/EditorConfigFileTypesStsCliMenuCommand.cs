using System.Net.Http;
using AppCliTools.CliMenu;
using LibSupportToolsServerWork.Cruders;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibSupportToolsServerWork.CliMenuCommands;

public sealed class EditorConfigFileTypesStsCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditorConfigFileTypesStsCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base("EditorConfig File Types", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        //ყოველ ჯერზე ახალი კრუდერი იქმნება, რომ ცვლილების შემდეგ სია სერვერიდან თავიდან ჩამოიტვირთოს
        var editorConfigFileTypesStsCruder =
            new EditorConfigFileTypesStsCruder(_logger, _httpClientFactory, _parametersManager);

        return editorConfigFileTypesStsCruder.GetListMenu();
    }
}
