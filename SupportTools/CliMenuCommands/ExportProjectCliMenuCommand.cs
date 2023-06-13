using System;
using System.IO;
using System.Linq;
using CliMenu;
using LibDataInput;
using LibMenuInput;
using LibParameters;
using Newtonsoft.Json;
using SupportTools.Models;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class ExportProjectCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    public ExportProjectCliMenuCommand(ParametersManager parametersManager, string projectName) : base(
        "Export Project", projectName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        try
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var projects = parameters.Projects;
            if (!projects.ContainsKey(_projectName))
            {
                StShared.WriteErrorLine($"Project {_projectName} does not found", true);
                return;
            }

            var project = projects[_projectName];

            var defCloneFile = project.ProjectFolderName is null
                ? null
                : Path.Combine(project.ProjectFolderName, $"Export{_projectName}.json");

            var fileWithExportData = MenuInputer.InputFilePath("File name for Export", defCloneFile, false);

            if (string.IsNullOrWhiteSpace(fileWithExportData))
            {
                StShared.WriteErrorLine("File name does not entered", true);
                return;
            }

            if (File.Exists(fileWithExportData))
                if (!Inputer.InputBool($"File {fileWithExportData} exists, overwrite?", false, false))
                    return;

            var projectExportData = new ProjectExportData(_projectName, project);
            foreach (var gitName in project.GitProjectNames.Where(gitName => parameters.Gits.ContainsKey(gitName)))
                projectExportData.Gits.Add(gitName, parameters.Gits[gitName]);


            var projectJsonText = JsonConvert.SerializeObject(projectExportData, Formatting.Indented);
            File.WriteAllText(fileWithExportData, projectJsonText);

            StShared.Pause();
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
    }
}