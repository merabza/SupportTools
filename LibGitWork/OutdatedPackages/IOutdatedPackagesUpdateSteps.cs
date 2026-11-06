using System.Threading;
using System.Threading.Tasks;

namespace LibGitWork.OutdatedPackages;

//Update Outdated Packages-ის გარე ნაბიჯები ცალკეა, რომ ტესტებმა git-ი და dotnet outdated არ გაუშვან
public interface IOutdatedPackagesUpdateSteps
{
    //projectName=null: ყველა პროექტის git-ები. false: რომელიმე ფოლდერი ვერ დასინქრონდა
    Task<bool> SyncGits(string? projectName, bool includeRemoteProjects, string commitMessage,
        CancellationToken cancellationToken);

    //false: პროექტის ფოლდერში ამ git-ის პაკეტები ვერ განახლდა
    bool UpdatePackages(string projectName, string gitName);
}
