using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsData.Models;

namespace SupportTools.Menu.DistributeAllPackages;

//„Distribute all packages“-ის ნაბიჯები, რომლებიც git-ს, dotnet-სა და პაკეტების სერვერს იძახებს.
//თანმიმდევრობას, შეტყობინებებსა და ჩავარდნისას გაჩერებას ბრძანება წყვეტს; ტესტები ნაბიჯების fake-ს აწვდიან
internal interface IPackageDistributionSteps
{
    //პროექტის გიტების სინქრონიზაცია; ცვლილებები ამ მესიჯით დაკომიტდება, კითხვის გარეშე
    Task<bool> SyncProjectGits(string projectName, string commitMessage, CancellationToken cancellationToken);

    //პროექტის build-ის შემოწმება; null - შედეგი არ ჩაწერილა
    ProjectBuildCheckResult? CheckBuild(string projectName, ProjectModel project, bool noIncremental,
        CancellationToken cancellationToken);

    //პაკეტის სოლუშენის შემოწმება
    bool CheckPackageSolution(string projectName);

    //პაკეტის დაბილდვა და ატვირთვა package manager-ზე
    bool BuildAndUploadPackage(string projectName);

    //ამ გაშვების პაკეტების გავრცელება პროექტში: ჯერ ყველა მათგანის კლონი ახლდება, შემდეგ ვერსიები იწევა და
    //რეფერენსები პაკეტებით იცვლება
    Task<bool> DistributePackagesToProject(string projectName, IReadOnlyList<string> packageProjectNames,
        CancellationToken cancellationToken);
}
