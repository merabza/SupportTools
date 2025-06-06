using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.ToolActions;

public sealed class ReCreateUpdateFrontSpaProjectToolAction : ToolAction
{
    private const string ActionName = "ReCreate Update Front Spa Project";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReCreateUpdateFrontSpaProjectToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base(logger, ActionName, null, null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //შეიქმნას ცარელა spa front პროექტი დროებით ფოლდერში

        //დავადგინოთ დროებითი ფოლდერის სახელი, სადაც უნდა შეიქმნას ფრონტის პროექტი
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        if (string.IsNullOrWhiteSpace(supportToolsParameters.TempFolder))
        {
            StShared.WriteErrorLine("TempFolder does not specified in parameters", true);
            return false;
        }

        var createInPath = supportToolsParameters.TempFolder;
        var projectFolderName = string.Empty;
        var projectFileName = string.Empty;
        var projectName = string.Empty;

        //რეაქტის პროექტის შექმნა ფრონტისთვის
        //var reactEsProjectCreator = new ReactEsProjectCreator(_logger, _httpClientFactory,
        //    projectForCreate.CreateInPath, projectForCreate.ProjectFolderName,
        //    projectForCreate.ProjectFileName, projectForCreate.ProjectName, true);
        //if (!reactEsProjectCreator.Create())
        //    return false;

        //დაინსტალირდეს პროექტის შესაბამისი npm პაკეტები

        //დაკოპირდეს პროექტის ფრონტის ფაილები დროებით ფოლდერში

        //დაკოპირდეს დროებით ფოლდერში შექმნილი პროექტის ფრონტის ფაილები მიმდინარე პროექტში

        return true;
    }
}