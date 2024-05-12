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

public sealed class ImportProjectCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ImportProjectCliMenuCommand(ParametersManager parametersManager) : base("Import Project")
    {
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        try
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var filenameForImport = MenuInputer.InputFilePath("File name for Import", null);

            if (!File.Exists(filenameForImport))
            {
                StShared.WriteErrorLine($"File {filenameForImport} does not exists", true);
                return;
            }

            var importData = File.ReadAllText(filenameForImport);


            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };


            var projectExportData = JsonConvert.DeserializeObject<ProjectExportData>(importData, settings);


            if (projectExportData is null)
            {
                StShared.WriteErrorLine($"Project data does not deserialized from file {filenameForImport}", true);
                return;
            }

            var projectName = projectExportData.ProjectName;
            if (parameters.Projects.ContainsKey(projectName))
            {
                StShared.WriteErrorLine($"Project with name {projectName} already exists", true);
                return;
            }

            var project = projectExportData.Project;
            project.ServerInfos.Clear();
            project.AllowToolsList.Clear();

            parameters.Projects.Add(projectName, projectExportData.Project);

            foreach (var git in projectExportData.Gits.Where(git => !parameters.Gits.ContainsKey(git.Key)))
                parameters.Gits.Add(git.Key, git.Value);

            _parametersManager.Save(parameters, $"Project {projectName} Added");
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            //StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
    }
}