using CliMenu;
using LibGitData.Models;
using LibMenuInput;
using LibParameters;
using SupportToolsData.Models;
using System;
using System.IO;
using System.Linq;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class LoadGitsFromCloneFileCommand : CloneInfoFileCliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public LoadGitsFromCloneFileCommand(ParametersManager parametersManager, string projectName) : base(
        "Load Gits From Clone File")
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override void RunAction()
    {

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
            return;
        }

        var mainProjectName = project.MainProjectName;
        string? mainProjectRelativePath = null;
        string? mainProjectFolderRelativePath = null;
        if (!string.IsNullOrWhiteSpace(mainProjectName) &&
            parameters.GitProjects.TryGetValue(mainProjectName, out var gitProject))
            mainProjectRelativePath = gitProject.ProjectRelativePath;
        if (!string.IsNullOrWhiteSpace(mainProjectRelativePath))
        {
            var fileInfo = new FileInfo(mainProjectRelativePath);
            mainProjectFolderRelativePath = fileInfo.Directory?.FullName + Path.DirectorySeparatorChar;
        }

        var defCloneFile = GetDefCloneFileName(parameters, project);

        var haveChanges = false;

        var fileWithCloneCommands = MenuInputer.InputFilePath("Enter file name with clone commands", defCloneFile);

        if (!File.Exists(fileWithCloneCommands))
        {
            StShared.WriteErrorLine($"File {fileWithCloneCommands} does not exists", true);
            return;
        }

        var lines = File.ReadLines(fileWithCloneCommands);
        const string gitCloneCommandStart = "git clone ";
        foreach (var line in lines)
        {
            var lineTrimmed = line.Trim();
            if (!lineTrimmed.StartsWith(gitCloneCommandStart))
                continue;

            var cloneParameters = lineTrimmed[gitCloneCommandStart.Length..].Trim();

            var pars = cloneParameters.Split(' ').Select(x => x.Trim()).Where(x => x != "").ToArray();

            if (pars.Length != 2)
                continue;

            //pars[0] გიტ პროექტის მოშორებული მისამართი
            //pars[1] ფოლდერის სახელი გიტის პროექტისთვის
            //გიტების ზოგად სიაში თუ არ არის ასეთი პროექტი დაემატოს.
            //პროექტის გიტის დასახელებებში დაემატოს ამ გიტის სახელი, ანუ pars[1]
            //დავიმახსოვროთ საჭიროა თუ არა შენახვა

            var gitProjectAddress = pars[0];
            var gitProjectFolderName = pars[1];

            //თუ ფოლდერი რამდენიმე სექციით არის მოცემული, მაშინ დავადგინოთ მასში მთავარი პროექტის ფოლდერის მონაწილეობა და მის ადგილას ჩავწეროთ {MainProjectPath}
            if (!string.IsNullOrWhiteSpace(mainProjectFolderRelativePath) &&
                gitProjectFolderName.StartsWith(mainProjectFolderRelativePath))
                gitProjectFolderName = GitDataModel.MainProjectFolderRelativePathName +
                                       gitProjectFolderName[mainProjectFolderRelativePath.Length..];


            var gitName = gitProjectFolderName;
            //თუ ფოლდერი რამდენიმე სექციით არის მოცემული, მაშინ გიტის რეპოზიტორიის სახელად დავიმახსოვროთ ბოლო სექცია
            var dir = new DirectoryInfo(gitName);
            gitName = dir.Name;


            var gitCountWithAddress = parameters.Gits.Count(x => x.Value.GitProjectAddress == gitProjectAddress);

            if (gitCountWithAddress == 0)
            {
                parameters.Gits.Add(gitName,
                    new GitDataModel
                        { GitProjectFolderName = gitProjectFolderName, GitProjectAddress = gitProjectAddress });
                haveChanges = true;
            }
            else
            {
                var gitKvp =
                    parameters.Gits.Single(x => x.Value.GitProjectAddress == gitProjectAddress);
                gitName = gitKvp.Key;
            }

            if (project.GitProjectNames.Contains(gitName))
                continue;

            project.GitProjectNames.Add(gitName);
            haveChanges = true;
        }

        //შევამოწმოთ აღმოჩენილია თუ არა ერთი მაინც გიტი, რომელიც ჩაემატა პროექტში.
        //თუ აღმოჩენილია, მოხდეს პარამეტრების ფაილის შენახვა.
        if (haveChanges)
            _parametersManager.Save(parameters, "Changed git projects");

        //GitSyncAll gitSyncAll = new GitSyncAll(_logger, project.ProjectFolderName,
        //    parameters.Gits.Where(x => project.GitProjectNames.Contains(x.Key)).Select(x => x.Value));
        //gitSyncAll.Run();

        MenuAction = EMenuAction.LevelUp;
        Console.WriteLine("Success");
    }
}