using SystemTools.SharedKernel;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//csproj ფაილის ცვლილებები, რომლებსაც პაკეტის გავრცელება dotnet-ის ბრძანებებით აკეთებს.
//ტესტები dotnet-ის გაშვების გარეშე fake-ს აწვდიან
internal interface IPackageReferenceCommands
{
    Result RemoveReferenceFromProject(string projectFilePath, string referenceProjectFilePath);

    Result AddPackageToProjectWithoutRestore(string projectFilePath, string packageName, string packageVersion);

    Result RestoreWithoutHttpCache(string projectFilePath);

    Result RemovePackageFromProject(string projectFilePath, string packageName);

    Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath);
}
