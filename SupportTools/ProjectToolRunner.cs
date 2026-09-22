using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools;

//მენიუს გვერდის ავლით, ბრძანებათა სტრიქონში მითითებული ინსტრუმენტის გაშვება მითითებული პროექტისათვის
public static class ProjectToolRunner
{
    //ორივე ჩამონათვალის ყველა ინსტრუმენტის სახელი, --run პარამეტრისათვის
    public static string[] GetAllToolNames()
    {
        return [.. Enum.GetNames<EProjectTools>(), .. Enum.GetNames<EProjectServerTools>()];
    }

    public static EProjectTools? FindProjectTool(string toolName)
    {
        return FindTool<EProjectTools>(toolName);
    }

    public static EProjectServerTools? FindServerTool(string toolName)
    {
        return FindTool<EProjectServerTools>(toolName);
    }

    //რეგისტრის გაუთვალისწინებლად ვეძებთ ინსტრუმენტს სახელით. რიცხვი სახელად არ ჩაითვლება
    private static T? FindTool<T>(string toolName) where T : struct, Enum
    {
        string? foundName = Enum.GetNames<T>()
            .FirstOrDefault(x => string.Equals(x, toolName, StringComparison.OrdinalIgnoreCase));

        return foundName is null ? null : Enum.Parse<T>(foundName);
    }

    public static async ValueTask<bool> Run(IServiceProvider serviceProvider, IParametersManager parametersManager,
        string projectName, EProjectTools tool, CancellationToken cancellationToken = default)
    {
        ProjectModel? project = GetProject(parametersManager, projectName);
        if (project is null)
        {
            return false;
        }

        //მენიუშიც მხოლოდ ნებადართული ინსტრუმენტები ჩანს, ამიტომ აქაც იგივე შეზღუდვა უნდა მოქმედებდეს
        List<EProjectTools> allowedTools = [.. Enum.GetValues<EProjectTools>().Intersect(project.AllowToolsList)];
        if (!allowedTools.Contains(tool))
        {
            StShared.WriteErrorLine(
                $"Tool {tool} is not allowed for project {projectName}. Allowed tools are: {NamesToString(allowedTools)}",
                true, null, false);
            return false;
        }

        IToolCommand? toolCommand =
            await ToolCommandFactory.CreateProjectToolCommand(tool, serviceProvider, parametersManager, projectName);

        return await RunToolCommand(toolCommand, $"{tool} for project {projectName}", cancellationToken);
    }

    public static async ValueTask<bool> RunOnServer(IServiceProvider serviceProvider,
        IParametersManager parametersManager, string projectName, string serverName, EProjectServerTools tool,
        CancellationToken cancellationToken = default)
    {
        ProjectModel? project = GetProject(parametersManager, projectName);
        if (project is null)
        {
            return false;
        }

        //სერვერს ვეძებთ როგორც ჩამონათვალის გასაღებით, ისე ServerName|EnvironmentName-ით და მხოლოდ სერვერის სახელითაც
        List<ServerInfoModel> foundServers =
        [
            .. project.ServerInfos.Where(kvp =>
                    string.Equals(kvp.Key, serverName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Value.GetItemKey(), serverName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Value.ServerName, serverName, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Value)
        ];

        string serversForMessage = string.Join(", ",
            project.ServerInfos.Values.Select(x => x.GetItemKey()).OrderBy(x => x, StringComparer.Ordinal));

        switch (foundServers.Count)
        {
            case 0:
                StShared.WriteErrorLine(
                    $"Server with name {serverName} not found for project {projectName}. Existing servers are: {serversForMessage}",
                    true, null, false);
                return false;
            case > 1:
                StShared.WriteErrorLine(
                    $"More than one server found with name {serverName} for project {projectName}. Use ServerName|EnvironmentName. Existing servers are: {serversForMessage}",
                    true, null, false);
                return false;
        }

        ServerInfoModel serverInfo = foundServers[0];

        List<EProjectServerTools> allowedTools =
            [.. Enum.GetValues<EProjectServerTools>().Intersect(serverInfo.AllowToolsList ?? [])];
        if (!allowedTools.Contains(tool))
        {
            StShared.WriteErrorLine(
                $"Tool {tool} is not allowed for project {projectName} on server {serverInfo.GetItemKey()}. Allowed tools are: {NamesToString(allowedTools)}",
                true, null, false);
            return false;
        }

        IToolCommand? toolCommand = await ToolCommandFactory.CreateProjectServerToolCommand(tool, serviceProvider,
            parametersManager, projectName, serverInfo, cancellationToken);

        return await RunToolCommand(toolCommand,
            $"{tool} for project {projectName} on server {serverInfo.GetItemKey()}", cancellationToken);
    }

    private static ProjectModel? GetProject(IParametersManager parametersManager, string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project is not null)
        {
            return project;
        }

        StShared.WriteErrorLine($"Project with name {projectName} not found", true, null, false);
        return null;
    }

    private static async ValueTask<bool> RunToolCommand(IToolCommand? toolCommand, string toolForMessage,
        CancellationToken cancellationToken)
    {
        if (toolCommand is not null)
        {
            return await toolCommand.Run(cancellationToken);
        }

        StShared.WriteErrorLine($"Tool {toolForMessage} could not be created", true, null, false);
        return false;
    }

    private static string NamesToString<T>(IEnumerable<T> tools) where T : struct, Enum
    {
        return string.Join(", ", tools.Select(x => x.ToString()).OrderBy(x => x, StringComparer.Ordinal));
    }
}
