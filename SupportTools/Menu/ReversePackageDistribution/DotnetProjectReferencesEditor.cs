using LibDotnetWork;
using SystemTools.SharedKernel;

namespace SupportTools.Menu.ReversePackageDistribution;

//ცვლილებები dotnet remove package / dotnet add reference / dotnet add package ბრძანებებით
internal sealed class DotnetProjectReferencesEditor : IProjectReferencesEditor
{
    private readonly DotnetProcessor _dotnetProcessor;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetProjectReferencesEditor(DotnetProcessor dotnetProcessor)
    {
        _dotnetProcessor = dotnetProcessor;
    }

    public Result RemovePackageFromProject(string projectFilePath, string packageName)
    {
        return _dotnetProcessor.RemovePackageFromProject(projectFilePath, packageName);
    }

    public Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
    {
        return _dotnetProcessor.AddReferenceToProject(projectFilePath, referenceProjectFilePath);
    }

    public Result AddPackageToProject(string projectFilePath, string packageName, string? packageVersion)
    {
        return _dotnetProcessor.AddPackageToProject(projectFilePath, packageName, packageVersion);
    }
}
