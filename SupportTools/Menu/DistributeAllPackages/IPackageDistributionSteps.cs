using System.Threading;
using System.Threading.Tasks;
using SupportToolsData.Models;

namespace SupportTools.Menu.DistributeAllPackages;

//„Distribute all packages“-ის ნაბიჯები, რომლებიც git-ს, dotnet-სა და პაკეტების სერვერს იძახებს.
//თანმიმდევრობას, შეტყობინებებსა და ჩავარდნისას გაჩერებას ბრძანება წყვეტს; ტესტები ნაბიჯების fake-ს აწვდიან
internal interface IPackageDistributionSteps
{
    //პროექტის გიტების სინქრონიზაცია
    Task<bool> SyncProjectGits(string projectName, CancellationToken cancellationToken);

    //პროექტის build-ის შემოწმება; null - შედეგი არ ჩაწერილა
    ProjectBuildCheckResult? CheckBuild(string projectName, ProjectModel project, bool noIncremental,
        CancellationToken cancellationToken);

    //პაკეტის სოლუშენის შემოწმება
    bool CheckPackageSolution(string projectName);

    //პაკეტის დაბილდვა და ატვირთვა package manager-ზე
    bool BuildAndUploadPackage(string projectName);

    //პაკეტის გავრცელება ყველა მომხმარებელ პროექტში
    Task<bool> DistributePackage(string projectName, CancellationToken cancellationToken);
}
