using SystemTools.SharedKernel;

namespace SupportTools.Menu.ReversePackageDistribution;

//csproj ფაილის ცვლილებები, რომლებსაც Reverse Package Distribution dotnet-ის ბრძანებებით აკეთებს.
//ტესტები dotnet-ის გაშვების გარეშე fake-ს აწვდიან
internal interface IProjectReferencesEditor
{
    Result RemovePackageFromProject(string projectFilePath, string packageName);

    Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath);

    Result AddPackageToProject(string projectFilePath, string packageName, string? packageVersion);
}
