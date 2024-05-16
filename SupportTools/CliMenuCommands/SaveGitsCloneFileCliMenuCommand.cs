using CliMenu;
using LibDataInput;
using LibMenuInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using System;
using System.IO;
using System.Text;
using LibGitData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class SaveGitsCloneFileCliMenuCommand : CloneInfoFileCliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SaveGitsCloneFileCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName) :
        base("Save Gits Clone File")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override void RunAction()
    {
        //1. დავადგინოთ Main Project File Name
        //2. გავიაროთ გიტი ყველა პროექტი და ვიპოვოთ გიტის ის პროექტი, რომლის ფოლდერიც შეიცავს Main Project File Name-ს ანუ არის ამ ფაილის მშობელი
        //3. ამ ნაპოვნი გიტის პროექტის მთავარ ფოლდერში შევქმნათ ფაილი, რომლის სახელიც იქნება CloneInfo.txt
        //საბოლოოდ მივედი იმ დასკვნამდე, რომ მთავარი პროექტის შესახებ ინფორმაციაც ისევე უნდა იყოს მიბმული პროექტზე, როგორც ბოლოს მიბმული ბაზის და სხვა პროექტები


        try
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var project = parameters.GetProject(_projectName);

            if (project is null)
            {
                StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
                return;
            }

            //var mainProjectName = project.MainProjectName;
            var defCloneFile = GetDefCloneFileName(parameters, project);

            var fileWithCloneCommands =
                MenuInputer.InputFilePath("Enter file name for save clone commands", defCloneFile, false);

            if (string.IsNullOrWhiteSpace(fileWithCloneCommands))
            {
                StShared.WriteErrorLine("File name does not entered", true);
                return;
            }

            if (File.Exists(fileWithCloneCommands))
                if (!Inputer.InputBool($"File {fileWithCloneCommands} exists, overwrite?", false, false))
                    return;

            StringBuilder sb = new();
            sb.AppendLine($"mkdir {_projectName}");
            sb.AppendLine($"cd {_projectName}");

            foreach (var gitProjectName in project.GitProjectNames)
            {
                if (!parameters.Gits.TryGetValue(gitProjectName, out var gitProject))
                {
                    StShared.WriteErrorLine($"Git project with name {gitProjectName} does not exists", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(gitProject.GitProjectAddress))
                {
                    StShared.WriteErrorLine(
                        $"GitProjectAddress does not specified for project with name {gitProjectName}", true);
                    return;
                }

                var gitProjectFolderName = gitProject.GitProjectFolderName;
                if (string.IsNullOrWhiteSpace(gitProjectFolderName))
                {
                    StShared.WriteErrorLine(
                        $"GitProjectFolderName does not specified for project with name {gitProjectName}", true);
                    return;
                }

                if (gitProjectFolderName.StartsWith(GitDataModel.MainProjectFolderRelativePathName))
                {
                    var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);
                    var mainProjectFolderRelativePath = project.MainProjectFolderRelativePath(gitProjects);
                    gitProjectFolderName = gitProjectFolderName.Replace(GitDataModel.MainProjectFolderRelativePathName,
                        mainProjectFolderRelativePath);
                }

                sb.AppendLine(
                    $"git clone {parameters.Gits[gitProjectName].GitProjectAddress} {gitProjectFolderName}");
            }

            sb.AppendLine("cd ..");

            File.WriteAllText(fileWithCloneCommands, sb.ToString());

            MenuAction = EMenuAction.LevelUp;
            Console.WriteLine("Success");
            return;
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
        //finally
        //{
        //    StShared.Pause();
        //}

        MenuAction = EMenuAction.Reload;
    }
}