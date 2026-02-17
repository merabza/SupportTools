using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using LibGitData.Models;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

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

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //1. დავადგინოთ Main Project File Name
        //2. გავიაროთ გიტი ყველა პროექტი და ვიპოვოთ გიტის ის პროექტი, რომლის ფოლდერიც შეიცავს Main Project File Name-ს ანუ არის ამ ფაილის მშობელი
        //3. ამ ნაპოვნი გიტის პროექტის მთავარ ფოლდერში შევქმნათ ფაილი, რომლის სახელიც იქნება CloneInfo.txt
        //საბოლოოდ მივედი იმ დასკვნამდე, რომ მთავარი პროექტის შესახებ ინფორმაციაც ისევე უნდა იყოს მიბმული პროექტზე, როგორც ბოლოს მიბმული ბაზის და სხვა პროექტები

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
            return false;
        }

        //var mainProjectName = project.MainProjectName;
        string? defCloneFile = GetDefCloneFileName(parameters, project);

        string? fileWithCloneCommands =
            MenuInputer.InputFilePath("Enter file name for save clone commands", defCloneFile, false);

        if (string.IsNullOrWhiteSpace(fileWithCloneCommands))
        {
            StShared.WriteErrorLine("File name does not entered", true);
            return false;
        }

        if (File.Exists(fileWithCloneCommands) &&
            !Inputer.InputBool($"File {fileWithCloneCommands} exists, overwrite?", false, false))
        {
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"mkdir {_projectName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"cd {_projectName}");

        foreach (string gitProjectName in project.GitProjectNames)
        {
            if (!parameters.Gits.TryGetValue(gitProjectName, out GitDataModel? gitProject))
            {
                StShared.WriteErrorLine($"Git project with name {gitProjectName} does not exists", true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(gitProject.GitProjectAddress))
            {
                StShared.WriteErrorLine($"GitProjectAddress does not specified for project with name {gitProjectName}",
                    true);
                return false;
            }

            string? gitProjectFolderName = gitProject.GitProjectFolderName;
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

            sb.AppendLine(CultureInfo.InvariantCulture,
                $"git clone {parameters.Gits[gitProjectName].GitProjectAddress} {gitProjectFolderName}");
        }

        sb.AppendLine("cd ..");

        await File.WriteAllTextAsync(fileWithCloneCommands, sb.ToString(), cancellationToken);

        MenuAction = EMenuAction.LevelUp;
        Console.WriteLine("Success");
        return true;
    }
}
