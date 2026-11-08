using System.Collections.Generic;
using SupportTools.Menu.ReversePackageDistribution;
using SystemTools.SharedKernel;

namespace SupportTools.Tests.Menu.ReversePackageDistribution;

//records the csproj changes of Reverse Package Distribution instead of running dotnet
internal sealed class FakeProjectReferencesEditor : IProjectReferencesEditor
{
    public List<string> Calls { get; } = [];
    public bool FailingRemove { get; set; }
    public bool FailingReference { get; set; }
    public bool FailingPackage { get; set; }

    public Result RemovePackageFromProject(string projectFilePath, string packageName)
    {
        Calls.Add($"remove {projectFilePath} {packageName}");
        return CreateResult(FailingRemove);
    }

    public Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
    {
        Calls.Add($"reference {projectFilePath} {referenceProjectFilePath}");
        return CreateResult(FailingReference);
    }

    public Result AddPackageToProject(string projectFilePath, string packageName, string? packageVersion)
    {
        Calls.Add($"package {projectFilePath} {packageName} {packageVersion}");
        return CreateResult(FailingPackage);
    }

    private static Result CreateResult(bool failing)
    {
        return failing ? Result.Failure(Error.Failure("Failed", "failed")) : Result.Success();
    }
}
