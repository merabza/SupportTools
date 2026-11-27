using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliParametersApiClientsEdit;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.SupportToolsParametersEdit;

//Api კლიენტის გამოყენების ადგილები: Support Tools Parameters Editor-ის SupportToolsServer-ისა და პაკეტების მენეჯერის
//კლიენტები, მონაცემთა ბაზასთან კავშირების ვებაგენტი, სერვერების ვებაგენტი, ინსტალატორის ვებაგენტი და პროექტების
//სერვერების შემოწმების ვებაგენტი. სახელი იმ მენიუს გზაა, რომელშიც ველი ჩანს, ბოლოში ველის სახელით (მომხმარებლის
//არჩევანი: ერთ მენიუში კლიენტის ორი ველი შეიძლება იყოს), ბმული კი ამ მენიუს გზაა. ინსტრუმენტები კლიენტს ზუსტი
//სახელით პოულობენ, ამიტომ აქაც ასე. დამალული ადგილიც ითვლება, როგორც DatabaseServerConnectionUsages-ში: მის სახელს
//ბოლოში დამმალავი პარამეტრი აწერია, ბმული კი იმ მენიუშია, სადაც ეს პარამეტრი ჩანს
public static class ApiClientUsages
{
    private static readonly string HideRemoteOnlyProjectsIsOn =
        $"({FieldName(nameof(SupportToolsParameters.HideRemoteOnlyProjects))} is on)";

    public static List<ApiClientUsage> Find(SupportToolsParameters parameters, string apiClientName)
    {
        List<ApiClientUsage> usages = [];

        //მთავარ მენიუში Support Tools Parameters Editor პირველია. მასში ჯერ საკუთარი ველებია, შემდეგ Database Server
        //Connections და Servers სიები, რედაქტორის რიგით
        const string editorLink = SupportToolsParametersEditor.MenuName;
        AddIfUses(usages, parameters.SupportToolsServerWebApiClientName, apiClientName, editorLink,
            nameof(SupportToolsParameters.SupportToolsServerWebApiClientName));
        AddIfUses(usages, parameters.LocalPackageManagerWebApiClientName, apiClientName, editorLink,
            nameof(SupportToolsParameters.LocalPackageManagerWebApiClientName));

        string connectionsLink = $"{editorLink}/{FieldName(nameof(SupportToolsParameters.DatabaseServerConnections))}";
        usages.AddRange(parameters.DatabaseServerConnections.Where(x => x.Value.DbWebAgentName == apiClientName)
            .OrderBy(x => x.Key).Select(x => CreateConnectionUsage($"{connectionsLink}/{x.Key}", x.Value)));

        string serversLink = $"{editorLink}/{FieldName(nameof(SupportToolsParameters.Servers))}";
        foreach ((string serverName, ServerDataModel server) in parameters.Servers.OrderBy(x => x.Key))
        {
            string serverLink = $"{serversLink}/{serverName}";
            AddIfUses(usages, server.WebAgentName, apiClientName, serverLink, nameof(ServerDataModel.WebAgentName));
            AddIfUses(usages, server.WebAgentInstallerName, apiClientName, serverLink,
                nameof(ServerDataModel.WebAgentInstallerName));
        }

        //ჯგუფები და მათი პროექტები მენიუს რიგით
        usages.AddRange(parameters.Projects
            .OrderBy(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName)).ThenBy(x => x.Key)
            .SelectMany(x => FindInProject(parameters, x.Key, x.Value, apiClientName)));

        return usages;
    }

    private static void AddIfUses(List<ApiClientUsage> usages, string? fieldValue, string apiClientName,
        string menuLink, string propertyName)
    {
        if (fieldValue == apiClientName)
        {
            usages.Add(new ApiClientUsage($"{menuLink}/{FieldName(propertyName)}", menuLink));
        }
    }

    //ვებაგენტის სახელი კავშირის მენიუში მხოლოდ WebAgent პროვაიდერისას ჩანს (DatabaseServerConnectionCruder),
    //პროვაიდერი კი იმავე მენიუშია
    private static ApiClientUsage CreateConnectionUsage(string connectionLink, DatabaseServerConnectionData connection)
    {
        string name = $"{connectionLink}/{FieldName(nameof(DatabaseServerConnectionData.DbWebAgentName))}";
        if (connection.DatabaseServerProvider == EDatabaseProvider.WebAgent)
        {
            return new ApiClientUsage(name, connectionLink);
        }

        string providerFieldName = FieldName(nameof(DatabaseServerConnectionData.DatabaseServerProvider));
        return new ApiClientUsage($"{name} ({providerFieldName} is {connection.DatabaseServerProvider})",
            connectionLink);
    }

    //პროექტის სერვერები პროექტის მენიუშია, თითოეული ServerName|EnvironmentName სახელით (ServerInfosListFactoryStrategy,
    //იმავე რიგით)
    private static IEnumerable<ApiClientUsage> FindInProject(SupportToolsParameters parameters, string projectName,
        ProjectModel project, string apiClientName)
    {
        string projectLink = $"{SupportToolsParameters.FixProjectGroupName(project.ProjectGroupName)}/{projectName}";
        return project.ServerInfos.Values.Where(x => x.WebAgentNameForCheck == apiClientName)
            .OrderBy(x => x.GetItemKey())
            .Select(x => CreateServerInfoUsage(parameters, project, projectLink, $"{projectLink}/{x.GetItemKey()}"));
    }

    private static ApiClientUsage CreateServerInfoUsage(SupportToolsParameters parameters, ProjectModel project,
        string projectLink, string serverInfoLink)
    {
        string name = $"{serverInfoLink}/{FieldName(nameof(ServerInfoModel.WebAgentNameForCheck))}";

        //HideRemoteOnlyProjects-ისას remote პროექტი ჯგუფების და პროექტების სიებში არ ჩანს (D3)
        if (parameters.HideRemoteOnlyProjects && !ProjectPresence.IsLocal(project))
        {
            return new ApiClientUsage($"{name} {HideRemoteOnlyProjectsIsOn}", SupportToolsParametersEditor.MenuName);
        }

        //შემოწმების ვებაგენტი მხოლოდ სერვისის სერვერის მენიუში ჩანს (ServerInfoCruder), პროექტის ტიპი კი პროექტის
        //მენიუშია
        if (project.ProjectType != EProjectType.IsService)
        {
            string projectTypeFieldName = FieldName(nameof(ProjectModel.ProjectType));
            return new ApiClientUsage($"{name} ({projectTypeFieldName} is {project.ProjectType})", projectLink);
        }

        return new ApiClientUsage(name, serverInfoLink);
    }

    //ველის რედაქტორის მენიუს ჩანაწერის სახელი, როცა რედაქტორს თავისი სახელი არ აქვს (FieldEditor.FieldName)
    private static string FieldName(string propertyName)
    {
        return propertyName.SplitWithSpacesCamelParts();
    }
}
