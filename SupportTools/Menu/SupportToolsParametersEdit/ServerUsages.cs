using System;
using System.Collections.Generic;
using System.Linq;
using SupportTools.Menu.CreateProject;
using SupportTools.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.SupportToolsParametersEdit;

//სერვერის გამოყენების ადგილები: ამ კომპიუტერის სერვერი (Current Machine Server Name), ახალი პროექტების შემქმნელის
//Production Server Name და პროექტების სერვერები. სახელი იმ მენიუს გზაა, რომელშიც სერვერი ჩანს, და ის ბმულიცაა.
//დამალული ადგილიც ითვლება, როგორც DatabaseServerConnectionUsages-ში: მის სახელს ბოლოში დამმალავი პარამეტრი აწერია,
//ბმული კი იმ მენიუშია, სადაც ეს პარამეტრი ჩანს
public static class ServerUsages
{
    private static readonly string HideRemoteOnlyProjectsIsOn =
        $"({nameof(SupportToolsParameters.HideRemoteOnlyProjects).SplitWithSpacesCamelParts()} is on)";

    public static List<ServerUsage> Find(SupportToolsParameters parameters, string serverName)
    {
        List<ServerUsage> usages = [];

        //მთავარ მენიუში Support Tools Parameters Editor პირველია. ამ კომპიუტერის სერვერს ServersIsLocalCalculator
        //რეგისტრის გარეშე ადარებს, ამიტომ აქაც ასე
        if (string.Equals(parameters.CurrentMachineServerName, serverName, StringComparison.OrdinalIgnoreCase))
        {
            usages.Add(new ServerUsage(SupportToolsParametersEditor.MenuName, SupportToolsParametersEditor.MenuName));
        }

        //Project Creator პროექტების ჯგუფებამდეა. პროექტის შემქმნელიც და ინსტრუმენტებიც სერვერს ზუსტი სახელით პოულობენ
        if (parameters.AppProjectCreatorAllParameters?.ProductionServerName == serverName)
        {
            usages.Add(new ServerUsage(ProjectCreatorSubMenuCliMenuCommand.MenuName,
                ProjectCreatorSubMenuCliMenuCommand.MenuName));
        }

        //ჯგუფები და მათი პროექტები მენიუს რიგით
        usages.AddRange(parameters.Projects
            .OrderBy(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName)).ThenBy(x => x.Key)
            .SelectMany(x => FindInProject(parameters, x.Key, x.Value, serverName)));

        return usages;
    }

    //პროექტის სერვერები პროექტის მენიუშია, თითოეული ServerName|EnvironmentName სახელით (ServerInfosListFactoryStrategy,
    //იმავე რიგით). სერვერის სახელი მის მენიუში Server Name ველია
    private static IEnumerable<ServerUsage> FindInProject(SupportToolsParameters parameters, string projectName,
        ProjectModel project, string serverName)
    {
        string projectLink = $"{SupportToolsParameters.FixProjectGroupName(project.ProjectGroupName)}/{projectName}";
        return project.ServerInfos.Values.Where(x => x.ServerName == serverName).OrderBy(x => x.GetItemKey())
            .Select(x => CreateUsage(parameters, project, $"{projectLink}/{x.GetItemKey()}"));
    }

    private static ServerUsage CreateUsage(SupportToolsParameters parameters, ProjectModel project, string path)
    {
        //HideRemoteOnlyProjects-ისას remote პროექტი ჯგუფების და პროექტების სიებში არ ჩანს (D3)
        if (parameters.HideRemoteOnlyProjects && !ProjectPresence.IsLocal(project))
        {
            return new ServerUsage($"{path} {HideRemoteOnlyProjectsIsOn}", SupportToolsParametersEditor.MenuName);
        }

        return new ServerUsage(path, path);
    }
}
