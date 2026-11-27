using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliParametersDataEdit.Models;
using ParametersManagement.LibDatabaseParameters;
using SupportTools.Menu.CreateProject;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.SupportToolsParametersEdit;

//მონაცემთა ბაზასთან კავშირის გამოყენების ადგილები: ახალი პროექტების შემქმნელის Developer Db Connection Name,
//პროექტების Dev და Prod Copy ბაზის პარამეტრები და პროექტის სერვერების Current და New ბაზის პარამეტრები.
//სახელი იმ მენიუს გზაა, რომელშიც კავშირი ჩანს, და ის ბმულიცაა. დამალული ადგილიც ითვლება (მომხმარებლის არჩევანი):
//მის სახელს ბოლოში დამმალავი პარამეტრი აწერია, ბმული კი იმ მენიუშია, სადაც ეს პარამეტრი ჩანს
public static class DatabaseServerConnectionUsages
{
    private static readonly string UsesDatabaseIsOff =
        $"({FieldName(nameof(ProjectModel.UsesDatabase))} is off)";

    private static readonly string HideRemoteOnlyProjectsIsOn =
        $"({FieldName(nameof(SupportToolsParameters.HideRemoteOnlyProjects))} is on)";

    public static List<DatabaseServerConnectionUsage> Find(SupportToolsParameters parameters, string connectionName)
    {
        List<DatabaseServerConnectionUsage> usages = [];

        //მთავარ მენიუში Project Creator პროექტების ჯგუფებამდეა
        if (parameters.AppProjectCreatorAllParameters?.DeveloperDbConnectionName == connectionName)
        {
            usages.Add(new DatabaseServerConnectionUsage(ProjectCreatorSubMenuCliMenuCommand.MenuName,
                ProjectCreatorSubMenuCliMenuCommand.MenuName));
        }

        //ჯგუფები და მათი პროექტები მენიუს რიგით
        usages.AddRange(parameters.Projects
            .OrderBy(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName)).ThenBy(x => x.Key)
            .SelectMany(x => FindInProject(parameters, x.Key, x.Value, connectionName)));

        return usages;
    }

    private static IEnumerable<DatabaseServerConnectionUsage> FindInProject(SupportToolsParameters parameters,
        string projectName, ProjectModel project, string connectionName)
    {
        string projectLink = $"{SupportToolsParameters.FixProjectGroupName(project.ProjectGroupName)}/{projectName}";
        return GetPlaces(projectLink, project).Where(x => x.DatabaseParameters?.DbConnectionName == connectionName)
            .Select(x => CreateUsage(parameters, project, projectLink, x.Path));
    }

    //პროექტის ბაზის პარამეტრების ველები პროექტის მენიუშია, სერვერისა კი სერვერის მენიუში, რომლის სახელი
    //ServerName|EnvironmentName-ია (ServerInfosListFactoryStrategy, იმავე რიგით)
    private static IEnumerable<(string Path, DatabaseParameters? DatabaseParameters)> GetPlaces(string projectLink,
        ProjectModel project)
    {
        yield return ($"{projectLink}/{FieldName(nameof(ProjectModel.DevDatabaseParameters))}",
            project.DevDatabaseParameters);
        yield return ($"{projectLink}/{FieldName(nameof(ProjectModel.ProdCopyDatabaseParameters))}",
            project.ProdCopyDatabaseParameters);

        foreach (ServerInfoModel serverInfo in project.ServerInfos.Values.OrderBy(x => x.GetItemKey()))
        {
            string serverLink = $"{projectLink}/{serverInfo.GetItemKey()}";
            yield return ($"{serverLink}/{FieldName(nameof(ServerInfoModel.CurrentDatabaseParameters))}",
                serverInfo.CurrentDatabaseParameters);
            yield return ($"{serverLink}/{FieldName(nameof(ServerInfoModel.NewDatabaseParameters))}",
                serverInfo.NewDatabaseParameters);
        }
    }

    private static DatabaseServerConnectionUsage CreateUsage(SupportToolsParameters parameters, ProjectModel project,
        string projectLink, string path)
    {
        //HideRemoteOnlyProjects-ისას remote პროექტი ჯგუფების და პროექტების სიებში არ ჩანს (D3)
        if (parameters.HideRemoteOnlyProjects && !ProjectPresence.IsLocal(project))
        {
            return new DatabaseServerConnectionUsage($"{path} {HideRemoteOnlyProjectsIsOn}",
                SupportToolsParametersEditor.MenuName);
        }

        //UsesDatabase-გამორთული პროექტის ბაზის პარამეტრები არც პროექტის და არც მისი სერვერების მენიუში არ ჩანს
        if (!project.ShowsDatabase())
        {
            return new DatabaseServerConnectionUsage($"{path} {UsesDatabaseIsOff}", projectLink);
        }

        return new DatabaseServerConnectionUsage(path, path);
    }

    //ველის რედაქტორის მენიუს ჩანაწერის სახელი, როცა რედაქტორს თავისი სახელი არ აქვს (FieldEditor.FieldName)
    private static string FieldName(string propertyName)
    {
        return propertyName.SplitWithSpacesCamelParts();
    }
}
