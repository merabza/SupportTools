using LibDotnetWork;
using SystemTools.SharedKernel;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;

//პაკეტების შექმნა და ატვირთვა dotnet pack / dotnet nuget push ბრძანებებით
internal sealed class DotnetPackageBuildCommands : IPackageBuildCommands
{
    private readonly DotnetProcessor _dotnetProcessor;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetPackageBuildCommands(DotnetProcessor dotnetProcessor)
    {
        _dotnetProcessor = dotnetProcessor;
    }

    public Result Pack(string solutionFileName, string outputFolderPath, string packageVersion)
    {
        return _dotnetProcessor.Pack(solutionFileName, outputFolderPath, packageVersion);
    }

    public Result NugetPush(string nupkgPath, string source, string? apiKey)
    {
        return _dotnetProcessor.NugetPush(nupkgPath, source, apiKey);
    }
}
