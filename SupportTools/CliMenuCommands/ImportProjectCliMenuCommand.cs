using System.IO;
using System.Linq;
using CliMenu;
using LibMenuInput;
using LibParameters;
using Newtonsoft.Json;
using SupportTools.Models;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class ImportProjectCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ImportProjectCliMenuCommand(ParametersManager parametersManager) : base("Import Project", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var filenameForImport = MenuInputer.InputFilePath("File name for Import", null);

        if (!File.Exists(filenameForImport))
        {
            StShared.WriteErrorLine($"File {filenameForImport} does not exists", true);
            return false;
        }

        var importData = File.ReadAllText(filenameForImport);


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

        var projectName = projectExportData.ProjectName;
        if (parameters.Projects.ContainsKey(projectName))
        {
            StShared.WriteErrorLine($"Project with name {projectName} already exists", true);
            return false;
        }

        var project = projectExportData.Project;
        project.ServerInfos.Clear();
        project.AllowToolsList.Clear();

        parameters.Projects.Add(projectName, projectExportData.Project);

        foreach (var git in projectExportData.Gits.Where(git => !parameters.Gits.ContainsKey(git.Key)))
            parameters.Gits.Add(git.Key, git.Value);

        _parametersManager.Save(parameters, $"Project {projectName} Added");
        return true;
    }
}