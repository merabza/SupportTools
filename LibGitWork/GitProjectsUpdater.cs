using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LibDataInput;
using LibGitData.Domain;
using LibGitData.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SqlServerDbTools;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork;

public sealed class GitProjectsUpdater
{
    private readonly GitRepos _gitRepos;
    private readonly string _gitsFolder;
    private readonly ILogger _logger;
    private readonly SupportToolsParameters _supportToolsParameters;
    private readonly string _workFolder;

    private GitProjectsUpdater(ILogger logger, SupportToolsParameters supportToolsParameters, string workFolder,
        string gitsFolder, GitRepos gitRepos)
    {
        _logger = logger;
        _supportToolsParameters = supportToolsParameters;
        _workFolder = workFolder;
        _gitsFolder = gitsFolder;
        _gitRepos = gitRepos;
    }

    public List<string> UsedProjectNames { get; } = [];

    public static GitProjectsUpdater? Create(ILogger logger, IParametersManager parametersManager)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var workFolder = supportToolsParameters.WorkFolder;
        if (string.IsNullOrWhiteSpace(workFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.WorkFolder does not specified", true, logger);
            return null;
        }

        var gitsFolder = Path.Combine(workFolder, "Gits");
        var gitRepos = GitRepos.Create(logger, supportToolsParameters.Gits, null, null);
        return new GitProjectsUpdater(logger, supportToolsParameters, workFolder, gitsFolder, gitRepos);
    }

    public bool ProcessOneGitProject(string gitName)
    {
        var git = _gitRepos.GetGitRepoByKey(gitName);
        if (git is null)
        {
            StShared.WriteErrorLine($"git with name {gitName} does not exists", true, _logger);
            return false;
        }

        var projectFolderName = GetProjectFolderName(git);
        if (!string.IsNullOrWhiteSpace(projectFolderName))
            return UpdateOneGitProject(projectFolderName, git) && ProcessFolder(projectFolderName, gitName);
        StShared.WriteErrorLine($"cannot count projectFolderName for git with name {gitName}", true, _logger);
        return false;

        /*

//https://stackoverflow.com/questions/7293008/display-last-git-commit-comment
//https://unix.stackexchange.com/questions/196952/get-last-commit-message-author-and-hash-using-git-ls-remote-like-command

         */

        //ჩამოტვირთული გიტის ფაილები გაანალიზდეს, იმისათვის, რომ დადგინდეს, რა პროექტები არის ამ ფაილებში
        //და რომელ პროექტებზეა დამოკიდებული ეს პროექტები
        //დადგენილი ინფორმაციის შენახვა მოხდეს პარამეტრებში

        //გავიაროთ projectFolderName ფოლდერი, თავისი ქვეფოლდერებით და მოვძებნოთ *.csproj ფაილები
    }

    private string? GetProjectFolderName(GitDataDomain git)
    {
        //შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        if (FileStat.CreateFolderIfNotExists(_workFolder, true) == null)
        {
            StShared.WriteErrorLine($"does not exists and cannot create work folder {_workFolder}", true, _logger);
            return null;
        }

        //შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერში Gits ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        //_gitsFolder = Path.Combine(_workFolder, "Gits");
        if (FileStat.CreateFolderIfNotExists(_gitsFolder, true) != null)
            return Path.Combine(_gitsFolder, git.GitProjectFolderName.Replace($"{Path.DirectorySeparatorChar}", "."));

        StShared.WriteErrorLine($"does not exists and cannot create work folder {_gitsFolder}", true, _logger);
        return null;

        //შემოწმდეს Gits ფოლდერში პროექტის ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
    }

    private bool UpdateOneGitProject(string projectFolderName, GitDataDomain git)
    {
        var gitProcessor = new GitProcessor(true, _logger, projectFolderName);

        if (Directory.Exists(projectFolderName))
        {
            //თუ ფოლდერი არსებობს, მაშინ დადგინდეს შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
            // თუ ეს ასე არ არის, წაიშალოს ფოლდერი თავისი შიგთავსით

            var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
            if (getRemoteOriginUrlResult.IsT1)
            {
                Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
                    SqlDbClientErrors.GetRemoteOriginUrlError));
                return false;
            }

            var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

            if (remoteOriginUrl.Trim(Environment.NewLine.ToCharArray()) != git.GitProjectAddress)
                Directory.Delete(projectFolderName, true);
        }

        //შემოწმდეს Gits ფოლდერში თუ არ არსებობს ფოლდერი gitDataModel.GitProjectFolderName სახელით,
        if (!Directory.Exists(projectFolderName))
        {
            //მოხდეს ამ Git პროექტის დაკლონვა შესაბამისი ფოლდერის სახელის მითითებით
            if (!gitProcessor.Clone(git.GitProjectAddress))
                return false;
        }
        else
        {
            //თუ ფოლდერი არსებობს, მოხდეს სტატუსის შემოწმება (იდეაში აქ ცვლილებები არ უნდა მომხდარიყო, მაგრამ მაინც)
            var needCommitResult = gitProcessor.NeedCommit();
            if (needCommitResult.IsT1)
            {
                Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1, SqlDbClientErrors.NeedCommitError));
                return false;
            }

            if (needCommitResult.AsT0)
            {
                //  თუ აღმოჩნდა რომ ცვლილებები მომხდარა, გამოვიდეს შეტყობინება ცვლილებების გაუქმებისა და თავიდან დაკლონვის შესახებ
                if (!Inputer.InputBool("Have changes, Restore to server version?", true))
                    return false;

                //     თანხმობის შემთხვევაში ან თავიდან დაიკლონოს, ან უკეთესია, თუ Checkout გაკეთდება.
                /*შემდეგი 3 ბრძანება თანმიმდევრობით იგივეა, რომ გიტის პროექტი თავიდან დაკლონო, ოღონდ მინიმალური ჩამოტვირთვით
                    git reset
                    git checkout .
                    git clean -fdx
                     */

                if (!gitProcessor.Reset())
                    return false;
                if (!gitProcessor.Checkout())
                    return false;
                if (!gitProcessor.Clean_fdx())
                    return false;
            }
            //შემოწმდეს ლოკალური ვერსია და remote ვერსია და თუ ერთნაირი არ არის გაკეთდეს git pull

            if (gitProcessor.GetGitState() != GitState.NeedToPull)
                return true;

            if (!gitProcessor.Pull())
                return false;
        }

        return true;
    }

    private bool ProcessFolder(string folderPath, string gitName)
    {
        Console.WriteLine($"(Finding csproj and esproj files) Process Folder {folderPath}");

        var dir = new DirectoryInfo(folderPath);

        if (dir.GetDirectories().Where(x => x.Name != ".git").Select(x => x.FullName)
            .Any(folderFullName => !ProcessFolder(folderFullName, gitName)))
            return false;

        var fileNames = Directory.GetFiles(folderPath, "*.csproj").ToList();
        fileNames.AddRange(Directory.GetFiles(folderPath, "*.esproj"));

        return fileNames.All(fileFullName => ProcessOneFile(fileFullName, gitName));
    }

    private bool ProcessOneFile(string filePath, string gitName)
    {
        Console.WriteLine($"Dependencies for {filePath}");
        var projectXml = XElement.Load(filePath);

        var projectReferences =
            projectXml.Descendants("ItemGroup").Descendants("ProjectReference").ToList();

        var projectRelativePath = Path.GetRelativePath(_gitsFolder, filePath);
        var project = RegisterProject(projectRelativePath, gitName);
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

    private GitProjectDataModel RegisterProject(string projectRelativePath, string gitName)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectRelativePath);
        if (!UsedProjectNames.Contains(projectName))
            UsedProjectNames.Add(projectName);

        _supportToolsParameters.GitProjects.TryAdd(projectName, new GitProjectDataModel
            { GitName = gitName, ProjectRelativePath = projectRelativePath });

        var gitProject = _supportToolsParameters.GitProjects[projectName];

        if (gitProject.GitName != gitName)
            gitProject.GitName = gitName;

        if (gitProject.ProjectRelativePath != projectRelativePath)
            gitProject.ProjectRelativePath = projectRelativePath;

        return _supportToolsParameters.GitProjects[projectName];
    }
}