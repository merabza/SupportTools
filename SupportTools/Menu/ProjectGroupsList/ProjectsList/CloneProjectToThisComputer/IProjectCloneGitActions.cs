using LibGitData;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//„Clone Project To This Computer“-ის git-ის ნაბიჯები. ტესტები git-ის გაშვების გარეშე fake-ს აწვდიან
internal interface IProjectCloneGitActions
{
    //Update Git Projects-ის სკანირება ერთი git-ისთვის: GitProjects-ში მისი csproj/esproj ჩანაწერები ემატება
    bool ScanGitProjects(string gitName);

    //ერთი git-ის კლონი (ან სინქრონიზაცია) პროექტის ფოლდერში არსებული V2 მექანიზმით
    void SyncGit(string projectName, EGitCol gitCol, string gitName);
}
