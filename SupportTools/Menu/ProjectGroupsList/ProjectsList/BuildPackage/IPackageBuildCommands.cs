using SystemTools.SharedKernel;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;

//პაკეტების შექმნა და ატვირთვა, რომლებსაც პაკეტის დაბილდვა dotnet-ის ბრძანებებით აკეთებს.
//ტესტები dotnet-ის გაშვების გარეშე fake-ს აწვდიან
internal interface IPackageBuildCommands
{
    Result Pack(string solutionFileName, string outputFolderPath, string packageVersion);

    Result NugetPush(string nupkgPath, string source, string? apiKey);
}
