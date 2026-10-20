using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

//სერვერის ჩანაწერის დაკოპირება არჩეულ პროექტში არჩეული სერვერისა და გარემოს სახელით. ასლს ახალი გასაღები ეძლევა,
//ისევე როგორც ახალ ჩანაწერს. თუ არჩეულ პროექტს ეს სერვერი და გარემო უკვე აქვს, არაფერი კოპირდება
public sealed class CopyServerInfoCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly Func<string, string?, CancellationToken, ValueTask<string?>> _selectEnvironmentName;
    private readonly Func<string, List<string>, string, string?> _selectProjectName;
    private readonly Func<string, string?, CancellationToken, ValueTask<string?>> _selectServerName;
    private readonly string _serverInfoKey;

    //სერვერისა და გარემოს სახელი ისევე აირჩევა, როგორც სერვერის ჩანაწერის რედაქტორში
    //(ServerDataNameFieldEditor, EnvironmentNameFieldEditor)
    public CopyServerInfoCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string projectName, string serverInfoKey) : this(parametersManager,
        projectName, serverInfoKey, SelectProjectName,
        (fieldName, currentServerName, cancellationToken) => ServerDataCruder
            .Create(logger, httpClientFactory, parametersManager)
            .GetNameWithPossibleNewName(fieldName, currentServerName, null, false, cancellationToken),
        (fieldName, currentEnvironmentName, cancellationToken) => EnvironmentCruder.Create(parametersManager)
            .GetNameWithPossibleNewName(fieldName, currentEnvironmentName, null, false, cancellationToken))
    {
    }

    //კონსოლიდან არჩევა პარამეტრებადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal CopyServerInfoCliMenuCommand(IParametersManager parametersManager, string projectName,
        string serverInfoKey, Func<string, List<string>, string, string?> selectProjectName,
        Func<string, string?, CancellationToken, ValueTask<string?>> selectServerName,
        Func<string, string?, CancellationToken, ValueTask<string?>> selectEnvironmentName) : base("Copy Server...",
        EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _serverInfoKey = serverInfoKey;
        _selectProjectName = selectProjectName;
        _selectServerName = selectServerName;
        _selectEnvironmentName = selectEnvironmentName;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ServerInfoModel? serverInfo = parameters.GetServerByProject(_projectName, _serverInfoKey);
        if (serverInfo is null)
        {
            StShared.WriteErrorLine($"Server with key {_serverInfoKey} does not exist in project {_projectName}", true);
            return false;
        }

        //ნაგულისხმევი მნიშვნელობებია მიმდინარე პროექტი, მიმდინარე სერვერი და მიმდინარე გარემო
        List<string> projectNames = [.. parameters.Projects.Keys.Order(StringComparer.OrdinalIgnoreCase)];
        string? targetProjectName = _selectProjectName("Project", projectNames, _projectName);
        ProjectModel? targetProject = targetProjectName is null ? null : parameters.GetProject(targetProjectName);
        if (targetProject is null)
        {
            StShared.WriteErrorLine($"Project {targetProjectName} does not exist", true);
            return false;
        }

        string? serverName = await _selectServerName("Server Name", serverInfo.ServerName, cancellationToken);
        string? environmentName =
            await _selectEnvironmentName("Environment Name", serverInfo.EnvironmentName, cancellationToken);

        //შედარება რეგისტრის გარეშეა, როგორც რეესტრის სინქრონიზაციაში ServerInfo-ს ბუნებრივი გასაღებისთვის: პროექტს,
        //რომლის ორი სერვერი მხოლოდ რეგისტრით განსხვავდება, სინქრონიზაცია სერვერზე აღარ გააგზავნის
        if (targetProject.ServerInfos.Values.Any(x =>
                string.Equals(x.ServerName, serverName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase)))
        {
            StShared.WriteErrorLine(
                $"Server {serverName} with environment {environmentName} already exists in project {targetProjectName}, nothing is copied",
                true);
            return false;
        }

        //ასლი იმავე სერიალიზაციით კეთდება, რომლითაც პარამეტრების ფაილი ინახება: ასე კოპირდება ყველა პარამეტრი და
        //ქვეპარამეტრი (მონაცემთა ბაზების პარამეტრები, დასაშვები ინსტრუმენტები), მოდელში მომავალში დამატებულიც
        ServerInfoModel copy =
            JsonConvert.DeserializeObject<ServerInfoModel>(JsonConvert.SerializeObject(serverInfo)) ??
            throw new InvalidOperationException($"Server with key {_serverInfoKey} is not copied");
        copy.ServerName = serverName;
        copy.EnvironmentName = environmentName;

        targetProject.ServerInfos.Add(Guid.NewGuid().ToString(), copy);

        return await _parametersManager.Save(parameters,
            $"Server {serverName}|{environmentName} copied to project {targetProjectName}", null, cancellationToken);
    }

    //პროექტები ბევრია და სია გვერდებად ჩანს: გვერდებს შორის გადასვლას MenuInputer.InputFromMenuList უჭერს მხარს,
    //SelectFromListInput კი არა
    private static string? SelectProjectName(string fieldName, List<string> projectNames, string currentProjectName)
    {
        var projectsMenuSet = new CliMenuSet();
        foreach (string projectName in projectNames)
        {
            projectsMenuSet.AddMenuItem(new CliMenuCommand(projectName));
        }

        return MenuInputer.InputFromMenuList(fieldName, projectsMenuSet, currentProjectName);
    }
}
