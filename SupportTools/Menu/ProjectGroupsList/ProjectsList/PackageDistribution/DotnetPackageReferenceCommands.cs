using LibDotnetWork;
using SystemTools.SharedKernel;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//ცვლილებები dotnet remove reference / add package --no-restore / restore --no-http-cache / remove package /
//add reference ბრძანებებით
internal sealed class DotnetPackageReferenceCommands : IPackageReferenceCommands
{
    private readonly DotnetProcessor _dotnetProcessor;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetPackageReferenceCommands(DotnetProcessor dotnetProcessor)
    {
        _dotnetProcessor = dotnetProcessor;
    }

    public Result RemoveReferenceFromProject(string projectFilePath, string referenceProjectFilePath)
    {
        return _dotnetProcessor.RemoveReferenceFromProject(projectFilePath, referenceProjectFilePath);
    }

    public Result AddPackageToProjectWithoutRestore(string projectFilePath, string packageName, string packageVersion)
    {
        return _dotnetProcessor.AddPackageToProjectWithoutRestore(projectFilePath, packageName, packageVersion);
    }

    public Result RestoreWithoutHttpCache(string projectFilePath)
    {
        return _dotnetProcessor.RestoreWithoutHttpCache(projectFilePath);
    }

    public Result RemovePackageFromProject(string projectFilePath, string packageName)
    {
        return _dotnetProcessor.RemovePackageFromProject(projectFilePath, packageName);
    }

    public Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
    {
        return _dotnetProcessor.AddReferenceToProject(projectFilePath, referenceProjectFilePath);
    }
}
