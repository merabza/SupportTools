using System;
using System.IO;
using System.Text;
using CliMenu;
using LibDataInput;
using LibMenuInput;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class SaveGitsCloneFileCliMenuCommand : CloneInfoFileCliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SaveGitsCloneFileCliMenuCommand(ParametersManager parametersManager, string projectName) : base(
        "Save Gits Clone File")
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override bool RunBody()
    {
        //1. დავადგინოთ Main Project File Name
        //2. გავიაროთ გიტი ყველა პროექტი და ვიპოვოთ გიტის ის პროექტი, რომლის ფოლდერიც შეიცავს Main Project File Name-ს ანუ არის ამ ფაილის მშობელი
        //3. ამ ნაპოვნი გიტის პროექტის მთავარ ფოლდერში შევქმნათ ფაილი, რომლის სახელიც იქნება CloneInfo.txt
        //საბოლოოდ მივედი იმ დასკვნამდე, რომ მთავარი პროექტის შესახებ ინფორმაციაც ისევე უნდა იყოს მიბმული პროექტზე, როგორც ბოლოს მიბმული ბაზის და სხვა პროექტები

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
            return false;
        }

        //var mainProjectName = project.MainProjectName;
        var defCloneFile = GetDefCloneFileName(parameters, project);

        var fileWithCloneCommands =
            MenuInputer.InputFilePath("Enter file name for save clone commands", defCloneFile, false);

        if (string.IsNullOrWhiteSpace(fileWithCloneCommands))
        {
            StShared.WriteErrorLine("File name does not entered", true);
            return false;
        }

        if (File.Exists(fileWithCloneCommands) &&
            !Inputer.InputBool($"File {fileWithCloneCommands} exists, overwrite?", false, false))
            return false;

        StringBuilder sb = new();
        sb.AppendLine($"mkdir {_projectName}");
        sb.AppendLine($"cd {_projectName}");

        foreach (var gitProjectName in project.GitProjectNames)
        {
            if (!parameters.Gits.TryGetValue(gitProjectName, out var gitProject))
            {
                StShared.WriteErrorLine($"Git project with name {gitProjectName} does not exists", true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(gitProject.GitProjectAddress))
            {
                StShared.WriteErrorLine(
                    $"GitProjectAddress does not specified for project with name {gitProjectName}", true);
                return false;
            }

            var gitProjectFolderName = gitProject.GitProjectFolderName;
            if (string.IsNullOrWhiteSpace(gitProjectFolderName))
            {
                StShared.WriteErrorLine(
                    $"GitProjectFolderName does not specified for project with name {gitProjectName}", true);
                return false;
            }

            //if (gitProjectFolderName.StartsWith(GitDataModel.MainProjectFolderRelativePathName))
            //{
            //    var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);
            //    var mainProjectFolderRelativePath = project.MainProjectFolderRelativePath(gitProjects);
            //    gitProjectFolderName = gitProjectFolderName.Replace(GitDataModel.MainProjectFolderRelativePathName,
            //        mainProjectFolderRelativePath);
            //}

            sb.AppendLine(
                $"git clone {parameters.Gits[gitProjectName].GitProjectAddress} {gitProjectFolderName}");
        }

        sb.AppendLine("cd ..");

        File.WriteAllText(fileWithCloneCommands, sb.ToString());

        MenuAction = EMenuAction.LevelUp;
        Console.WriteLine("Success");
        return true;
    }
}