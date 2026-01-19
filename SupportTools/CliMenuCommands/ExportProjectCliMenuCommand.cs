using System.IO;
using System.Linq;
using CliMenu;
using LibDataInput;
using LibMenuInput;
using Newtonsoft.Json;
using ParametersManagement.LibParameters;
using SupportTools.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class ExportProjectCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ExportProjectCliMenuCommand(ParametersManager parametersManager, string projectName) : base("Export Project",
        EMenuAction.Reload, EMenuAction.Reload, projectName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override bool RunBody()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var projects = parameters.Projects;
        if (!projects.TryGetValue(_projectName, out var project))
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return false;
        }

        var defCloneFile = project.ProjectFolderName is null
            ? null
            : Path.Combine(project.ProjectFolderName, $"Export{_projectName}.json");

        var fileWithExportData = MenuInputer.InputFilePath("File name for Export", defCloneFile, false);

        if (string.IsNullOrWhiteSpace(fileWithExportData))
        {
            StShared.WriteErrorLine("File name does not entered", true);
            return false;
        }

        if (File.Exists(fileWithExportData) &&
            !Inputer.InputBool($"File {fileWithExportData} exists, overwrite?", false, false))
            return false;

        var projectExportData = new ProjectExportData(_projectName, project);
        foreach (var gitName in project.GitProjectNames.Where(gitName => parameters.Gits.ContainsKey(gitName)))
            projectExportData.Gits.Add(gitName, parameters.Gits[gitName]);

        var projectJsonText = JsonConvert.SerializeObject(projectExportData, Formatting.Indented);
        File.WriteAllText(fileWithExportData, projectJsonText);

        return true;
    }
}