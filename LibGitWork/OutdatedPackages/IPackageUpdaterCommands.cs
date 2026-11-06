using SystemTools.SharedKernel;

namespace LibGitWork.OutdatedPackages;

//dotnet-ის ბრძანებები, რომლებსაც PackageUpdaterToolAction უშვებს. ცალკეა, რომ ტესტებმა restore და dotnet outdated არ
//გაუშვან
internal interface IPackageUpdaterCommands
{
    Result RestoreForOutdated(string solutionFileFullName);

    Result<(string, int)> UpdateOutdatedPackages(string projectFolderName);

    Result RestoreAfterOutdated(string solutionFileFullName);
}
