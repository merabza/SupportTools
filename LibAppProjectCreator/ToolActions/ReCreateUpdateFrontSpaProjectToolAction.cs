using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator.AppCreators;
using LibNpmWork;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.ToolActions;

public sealed class ReCreateUpdateFrontSpaProjectToolAction : ToolAction
{
    private const string ActionName = "ReCreate Update Front Spa Project";
    private const string FrontSpaProjects = nameof(FrontSpaProjects);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReCreateUpdateFrontSpaProjectToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string projectName) : base(logger, ActionName, null, null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
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

        var project = supportToolsParameters.GetProjectRequired(_projectName);

        if (string.IsNullOrWhiteSpace(project.SpaProjectName))
        {
            StShared.WriteErrorLine("SpaProjectName does not specified in parameters", true);
            return false;
        }

        var createInPath = Path.Combine(supportToolsParameters.TempFolder, FrontSpaProjects, _projectName,
            $"{_projectName}Front");

        //რეაქტის პროექტის შექმნა ფრონტისთვის
        var reactEsProjectCreator = new ReactEsProjectCreator(_logger, _httpClientFactory, createInPath, project.SpaProjectName,
            $"{project.SpaProjectName}.esproj", project.SpaProjectName, true);
        
        if (Directory.Exists(createInPath))
            FileStat.DeleteDirectoryWithNormaliseAttributes(createInPath);

        if (!reactEsProjectCreator.Create())
            return false;

        var npmProcessor = new NpmProcessor(_logger);
        var spaProjectPath = Path.Combine(createInPath, project.SpaProjectName);

        if (!npmProcessor.InstallNpmPackages(spaProjectPath))
            return false;

        //დაინსტალირდეს პროექტის შესაბამისი npm პაკეტები
        foreach (var npmPackageName in project.FrontNpmPackageNames)
        {
            if (!npmProcessor.InstallNpmPackage(spaProjectPath, npmPackageName))
                return false;
        }

        //დაკოპირდეს პროექტის ფრონტის ფაილები დროებით ფოლდერში

        //დაკოპირდეს დროებით ფოლდერში შექმნილი პროექტის ფრონტის ფაილები მიმდინარე პროექტში

        return true;
    }
}