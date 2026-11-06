using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibGitWork.OutdatedPackages;

//Update Outdated Packages-ის გარე ნაბიჯები ცალკეა, რომ ტესტებმა git-ი და dotnet outdated არ გაუშვან
public interface IOutdatedPackagesUpdateSteps
{
    //projectName=null: ყველა პროექტის git-ები. false: რომელიმე ფოლდერი ვერ დასინქრონდა
    Task<bool> SyncGits(string? projectName, bool includeRemoteProjects, string commitMessage,
        CancellationToken cancellationToken);

    //ერთი დონის git-ების პაკეტების განახლება, თითოეული თავისი პროექტის ფოლდერში, პარალელურად. შედეგები ნაბიჯების რიგით
    IReadOnlyList<EPackageUpdateResult> UpdatePackages(IReadOnlyList<OutdatedPackagesUpdateStep> steps,
        CancellationToken cancellationToken);
}
