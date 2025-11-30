using System;
using System.IO;
using LibDataInput;
using Microsoft.Extensions.Logging;
using SqlServerDbTools.Errors;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared.Errors;

namespace LibGitWork;

public sealed class GitOneProjectUpdater
{
    private readonly StsGitDataModel _git;
    private readonly ILogger? _logger;
    private readonly string _projectPath;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitOneProjectUpdater(ILogger? logger, string projectPath, StsGitDataModel git)
    {
        _logger = logger;
        _projectPath = projectPath;
        _git = git;
    }

    public GitProcessor? UpdateOneGitProject()
    {
        var gitProcessor = new GitProcessor(true, _logger, _projectPath);

        if (Directory.Exists(_projectPath))
        {
            //თუ ფოლდერი არსებობს, მაშინ დადგინდეს შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
            // თუ ეს ასე არ არის, წაიშალოს ფოლდერი თავისი შიგთავსით

            var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
            if (getRemoteOriginUrlResult.IsT1)
            {
                Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
                    SqlDbClientErrors.GetRemoteOriginUrlError));
                return null;
            }

            var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

            if (remoteOriginUrl.Trim(Environment.NewLine.ToCharArray()) != _git.GitProjectAddress)
                Directory.Delete(_projectPath, true);
        }

        //შემოწმდეს Gits ფოლდერში თუ არ არსებობს ფოლდერი gitDataModel.GitProjectFolderName სახელით,
        if (!Directory.Exists(_projectPath))
        {
            //მოხდეს ამ Git პროექტის დაკლონვა შესაბამისი ფოლდერის სახელის მითითებით
            if (!gitProcessor.Clone(_git.GitProjectAddress))
                return null;
        }
        else
        {
            //თუ ფოლდერი არსებობს, მოხდეს სტატუსის შემოწმება (იდეაში აქ ცვლილებები არ უნდა მომხდარიყო, მაგრამ მაინც)
            var needCommitResult = gitProcessor.NeedCommit();
            if (needCommitResult.IsT1)
            {
                Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1, SqlDbClientErrors.NeedCommitError));
                return null;
            }

            if (needCommitResult.AsT0)
            {
                //  თუ აღმოჩნდა რომ ცვლილებები მომხდარა, გამოვიდეს შეტყობინება ცვლილებების გაუქმებისა და თავიდან დაკლონვის შესახებ
                if (!Inputer.InputBool(
                        $"{_projectPath} Have changes it is SupportTools Work folder and must be Restored to server version. continue?",
                        true))
                    return null;

                //     თანხმობის შემთხვევაში ან თავიდან დაიკლონოს, ან უკეთესია, თუ Checkout გაკეთდება.
                /*შემდეგი 3 ბრძანება თანმიმდევრობით იგივეა, რომ გიტის პროექტი თავიდან დაკლონო, ოღონდ მინიმალური ჩამოტვირთვით
                    git reset
                    git checkout .
                    git clean -fdx
                     */

                if (!gitProcessor.Reset())
                    return null;
                if (!gitProcessor.Checkout())
                    return null;
                if (!gitProcessor.Clean_fdx())
                    return null;
            }
            else
            {
                if (!gitProcessor.GitRemoteUpdate())
                    return null;

                //შემოწმდეს ლოკალური ვერსია და remote ვერსია და თუ ერთნაირი არ არის გაკეთდეს git pull
                if (gitProcessor.GetGitState() == GitState.NeedToPull && !gitProcessor.Pull())
                    return null;
            }
        }

        return gitProcessor;
    }
}