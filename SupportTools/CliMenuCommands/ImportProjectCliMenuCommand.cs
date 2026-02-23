using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using LibGitData.Models;
using Newtonsoft.Json;
using ParametersManagement.LibParameters;
using SupportTools.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class ImportProjectCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ImportProjectCliMenuCommand(ParametersManager parametersManager) : base("Import Project", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? filenameForImport = MenuInputer.InputFilePath("File name for Import", null);

        if (!File.Exists(filenameForImport))
        {
            StShared.WriteErrorLine($"File {filenameForImport} does not exists", true);
            return false;
        }

        string importData = await File.ReadAllTextAsync(filenameForImport, cancellationToken);

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore
        };

        var projectExportData = JsonConvert.DeserializeObject<ProjectExportData>(importData, settings);

        if (projectExportData is null)
        {
            StShared.WriteErrorLine($"Project data does not deserialized from file {filenameForImport}", true);
            return false;
        }

        string projectName = projectExportData.ProjectName;
        if (parameters.Projects.ContainsKey(projectName))
        {
            StShared.WriteErrorLine($"Project with name {projectName} already exists", true);
            return false;
        }

        ProjectModel project = projectExportData.Project;
        project.ServerInfos.Clear();
        project.AllowToolsList.Clear();

        parameters.Projects.Add(projectName, projectExportData.Project);

        foreach (KeyValuePair<string, GitDataModel> git in projectExportData.Gits.Where(git =>
                     !parameters.Gits.ContainsKey(git.Key)))
        {
            parameters.Gits.Add(git.Key, git.Value);
        }

        await _parametersManager.Save(parameters, $"Project {projectName} Added", null, cancellationToken);
        return true;
    }
}
