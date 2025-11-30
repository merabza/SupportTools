using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LibGitData.Models;
using LibGitWork.Helpers;
using LibGitWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork;

public sealed class GitProjectsUpdater
{
    private readonly GitData _gitData;
    private readonly string _gitName;
    private readonly string _gitsFolder;
    private readonly ILogger? _logger;
    private readonly string _projectFolderName;
    private readonly SupportToolsParameters _supportToolsParameters;

    private GitProjectsUpdater(ILogger? logger, SupportToolsParameters supportToolsParameters, string gitsFolder,
        GitData gitData, string projectFolderName, string gitName)
    {
        _logger = logger;
        _supportToolsParameters = supportToolsParameters;
        _gitsFolder = gitsFolder;
        _gitData = gitData;
        _projectFolderName = projectFolderName;
        _gitName = gitName;
    }

    public string? LastRemoteId { get; private set; }

    public List<string> UsedProjectNames { get; } = [];

    public static GitProjectsUpdater? Create(ILogger? logger, IParametersManager parametersManager, string gitName,
        bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var workFolder = supportToolsParameters.WorkFolder;
        if (string.IsNullOrWhiteSpace(workFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.WorkFolder does not specified", true, logger);
            return null;
        }

        var gitsFolder = Path.Combine(workFolder, "Gits");
        var gitRepos = GitRepos.Create(logger, supportToolsParameters.Gits, null, useConsole, true);

        var gitData = gitRepos.GetGitRepoByKey(gitName);
        if (gitData is null)
        {
            StShared.WriteErrorLine($"git with name {gitName} does not exists", true, logger);
            return null;
        }

        var projectFolderName = GitFolderCountHelper.GetProjectFolderName(logger, workFolder, gitsFolder, gitData);
        if (!string.IsNullOrWhiteSpace(projectFolderName))
            return new GitProjectsUpdater(logger, supportToolsParameters, gitsFolder, gitData, projectFolderName,
                gitName);

        StShared.WriteErrorLine($"cannot count projectFolderName for git with name {gitName}", true, logger);
        return null;
    }

    public GitProcessor? ProcessOneGitProject(bool processFolder = true)
    {
        var gitOneProjectUpdater = new GitOneProjectUpdater(_logger, _projectFolderName, _gitData);
        var gitProcessor = gitOneProjectUpdater.UpdateOneGitProject();

        if (gitProcessor is null)
            return null;

        gitProcessor.CheckRemoteId();
        LastRemoteId = gitProcessor.LastRemoteId;

        if (!processFolder)
            return gitProcessor;
        return !ProcessFolder(_projectFolderName, _gitName) ? null : gitProcessor;

        /*

//https://stackoverflow.com/questions/7293008/display-last-git-commit-comment
//https://unix.stackexchange.com/questions/196952/get-last-commit-message-author-and-hash-using-git-ls-remote-like-command

         */

        //ჩამოტვირთული გიტის ფაილები გაანალიზდეს, იმისათვის, რომ დადგინდეს, რა პროექტები არის ამ ფაილებში
        //და რომელ პროექტებზეა დამოკიდებული ეს პროექტები
        //დადგენილი ინფორმაციის შენახვა მოხდეს პარამეტრებში

        //გავიაროთ projectFolderName ფოლდერი, თავისი ქვეფოლდერებით და მოვძებნოთ *.csproj ფაილები
    }

    //private GitProcessor? UpdateOneGitProject(string projectFolderName, GitDataDomain git)
    //{
    //    var gitProcessor = new GitProcessor(true, _logger, projectFolderName);

    //    if (Directory.Exists(projectFolderName))
    //    {
    //        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
    //        // თუ ეს ასე არ არის, წაიშალოს ფოლდერი თავისი შიგთავსით

    //        var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
    //        if (getRemoteOriginUrlResult.IsT1)
    //        {
    //            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
    //                SqlDbClientErrors.GetRemoteOriginUrlError));
    //            return null;
    //        }

    //        var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

    //        if (remoteOriginUrl.Trim(Environment.NewLine.ToCharArray()) != git.GitProjectAddress)
    //            Directory.Delete(projectFolderName, true);
    //    }

    //    //შემოწმდეს Gits ფოლდერში თუ არ არსებობს ფოლდერი gitDataModel.GitProjectFolderName სახელით,
    //    if (!Directory.Exists(projectFolderName))
    //    {
    //        //მოხდეს ამ Git პროექტის დაკლონვა შესაბამისი ფოლდერის სახელის მითითებით
    //        if (!gitProcessor.Clone(git.GitProjectAddress))
    //            return null;
    //    }
    //    else
    //    {
    //        //თუ ფოლდერი არსებობს, მოხდეს სტატუსის შემოწმება (იდეაში აქ ცვლილებები არ უნდა მომხდარიყო, მაგრამ მაინც)
    //        var needCommitResult = gitProcessor.NeedCommit();
    //        if (needCommitResult.IsT1)
    //        {
    //            Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1, SqlDbClientErrors.NeedCommitError));
    //            return null;
    //        }

    //        if (needCommitResult.AsT0)
    //        {
    //            //  თუ აღმოჩნდა რომ ცვლილებები მომხდარა, გამოვიდეს შეტყობინება ცვლილებების გაუქმებისა და თავიდან დაკლონვის შესახებ
    //            if (!Inputer.InputBool(
    //                    $"{projectFolderName} Have changes it is SupportTools Work folder and must be Restored to server version. continue?",
    //                    true))
    //                return null;

    //            //     თანხმობის შემთხვევაში ან თავიდან დაიკლონოს, ან უკეთესია, თუ Checkout გაკეთდება.
    //            /*შემდეგი 3 ბრძანება თანმიმდევრობით იგივეა, რომ გიტის პროექტი თავიდან დაკლონო, ოღონდ მინიმალური ჩამოტვირთვით
    //                git reset
    //                git checkout .
    //                git clean -fdx
    //                 */

    //            if (!gitProcessor.Reset())
    //                return null;
    //            if (!gitProcessor.Checkout())
    //                return null;
    //            if (!gitProcessor.Clean_fdx())
    //                return null;
    //        }
    //        else
    //        {
    //            if (!gitProcessor.GitRemoteUpdate())
    //                return null;

    //            //შემოწმდეს ლოკალური ვერსია და remote ვერსია და თუ ერთნაირი არ არის გაკეთდეს git pull
    //            if (gitProcessor.GetGitState() == GitState.NeedToPull && !gitProcessor.Pull())
    //                return null;
    //        }
    //    }

    //    gitProcessor.CheckRemoteId();
    //    LastRemoteId = gitProcessor.LastRemoteId;
    //    return gitProcessor;
    //}

    private bool ProcessFolder(string folderPath, string gitName)
    {
        Console.WriteLine($"(Finding csproj and esproj files) Process Folder {folderPath}");

        var dir = new DirectoryInfo(folderPath);

        if (dir.GetDirectories().Where(x => x.Name != ".git").Select(x => x.FullName)
            .Any(folderFullName => !ProcessFolder(folderFullName, gitName)))
            return false;

        //var fileNames = Directory.GetFiles(folderPath, "*.csproj").ToList();
        //fileNames.AddRange(Directory.GetFiles(folderPath, "*.esproj"));

        var fileNames = dir.GetFiles("*.csproj").Select(x => x.Name).ToList();
        fileNames.AddRange(dir.GetFiles("*.esproj").Select(x => x.Name));

        return fileNames.All(fileName => ProcessOneFile(folderPath, fileName, gitName));
    }

    private bool ProcessOneFile(string folderPath, string fileName, string gitName)
    {
        var filePath = Path.Combine(folderPath, fileName);
        Console.WriteLine($"Dependencies for {filePath}");
        var projectXml = XElement.Load(filePath);

        var projectReferences = projectXml.Descendants("ItemGroup").Descendants("ProjectReference").ToList();

        var projectRelativePath = Path.GetRelativePath(_gitsFolder, folderPath);
        var project = RegisterProject(projectRelativePath, fileName, gitName);
        List<string> dependsOnProjectNames = [];

        foreach (var element in projectReferences)
        {
            var attributes = element.Attributes("Include");
            foreach (var attr in attributes)
            {
                var fileFolderName = Path.GetDirectoryName(filePath);
                if (fileFolderName is null)
                    return false;
                var depProjectFullPath = new DirectoryInfo(Path.Combine(fileFolderName, attr.Value)).FullName;

                var depProjectRelativePath = Path.GetRelativePath(_gitsFolder, depProjectFullPath);
                //გამოყენებული პროექტების ესე დარეგისტრირება საჭირო არ არის, რადგან ყველა პროექტი მაინც დარეგისტრირდება თავის ადგილას
                //(string depProjectName, _) = RegisterProject(depProjectRelativePath, gitName);

                var depProjectName = Path.GetFileNameWithoutExtension(depProjectRelativePath);

                if (!dependsOnProjectNames.Contains(depProjectName))
                    dependsOnProjectNames.Add(depProjectName);
            }
        }

        project.DependsOn(dependsOnProjectNames);

        Console.WriteLine();
        return true;
    }

    private GitProjectDataModel RegisterProject(string projectRelativePath, string projectFileName, string gitName)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectFileName);
        if (!UsedProjectNames.Contains(projectName))
            UsedProjectNames.Add(projectName);

        _supportToolsParameters.GitProjects.TryAdd(projectName,
            new GitProjectDataModel
            {
                GitName = gitName, ProjectRelativePath = projectRelativePath, ProjectFileName = projectFileName
            });

        var gitProject = _supportToolsParameters.GitProjects[projectName];

        if (gitProject.GitName != gitName)
            gitProject.GitName = gitName;

        if (gitProject.ProjectRelativePath != projectRelativePath)
            gitProject.ProjectRelativePath = projectRelativePath;

        if (gitProject.ProjectFileName != projectFileName)
            gitProject.ProjectFileName = projectFileName;

        return _supportToolsParameters.GitProjects[projectName];
    }
}